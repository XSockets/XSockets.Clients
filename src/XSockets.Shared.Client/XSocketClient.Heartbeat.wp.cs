#if WINDOWS_UWP || WINDOWS_PHONE_APP

namespace XSockets
{
    using System;
    using System.Linq;
    using System.Text;
    using XSockets.Common.Interfaces;
    using System.Threading;
    using System.Threading.Tasks;

    internal class DispatcherTimer : CancellationTokenSource, IDisposable
    {
        internal DispatcherTimer(Action callback, int period)
        {
            Task.Delay(period).ContinueWith(async (t, s) =>
            {
                var tuple = (Tuple<Action, object>) s;

                while (true)
                {
                    if (IsCancellationRequested)
                        break;
                    Task.Run(() => tuple.Item1());
                    await Task.Delay(period);
                }

            }, Tuple.Create(callback), CancellationToken.None,
                TaskContinuationOptions.ExecuteSynchronously | TaskContinuationOptions.OnlyOnRanToCompletion,
                TaskScheduler.Default);
        }

        public new void Dispose() { base.Cancel(); }
    }
    public partial class XSocketClient : IXSocketClient
    {
        internal DispatcherTimer _heartbeatTimer;        

        private void StopHeartbeat()
        {
            if (this._heartbeatTimer != null)
            {
                this._heartbeatTimer.Dispose();
            }
        }
        public virtual void SetAutoHeartbeat(int timeoutInMs = 30000)
        {
            if (timeoutInMs <= 0)
            {
                AutoHeartbeat = false;
                _autoHeartbeatTimeout = 0;
            }
            else
            {
                AutoHeartbeat = true;
                _autoHeartbeatTimeout = timeoutInMs;
            }
        }
        private void StartHeartbeat(int intervallMs = 30000)
        {
            if (!this.AutoHeartbeat) return;

            if (!this.IsConnected)
                throw new Exception("You can't start the hearbeat before you have a connection.");

            _lastPong = DateTime.Now;
            this.OnPong += async (s, m) =>
            {
                //Got a pong back... set time for received pong.
                this._lastPong = DateTime.Now;

                var b = m.Blob.ToArray();
                if (Encoding.UTF8.GetString(b,0,b.Length) != this.PersistentId.ToString())
                {
                    await this.Disconnect();
                }
            };
            //Call ping on interval...
            this._heartbeatTimer = new DispatcherTimer(async () =>
            {
                if (this._lastPong < DateTime.Now.AddMilliseconds(-(intervallMs * 2)))
                {
                    await this.Disconnect();                    
                    this._heartbeatTimer.Dispose();
                    return;
                }

                await this.Ping(Encoding.UTF8.GetBytes(this.PersistentId.ToString()));
            },intervallMs);        
        }
    }
}
#endif