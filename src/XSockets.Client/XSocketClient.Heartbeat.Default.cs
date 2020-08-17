namespace XSockets
{
    using System;
    using System.Linq;
    using System.Text;
    using Common.Interfaces;
    using System.Timers;
    public partial class XSocketClient : IXSocketClient
    {
        private Timer _hearbeatTimer;

        private void StopHeartbeat()
        {
            if (this._hearbeatTimer != null && this._hearbeatTimer.Enabled)
            {
                this._hearbeatTimer.Stop();
                this._hearbeatTimer.Dispose();
            }
        }
        /// <summary>
        /// Pass in 0 (or less) to disable the AutoHeartbeat
        /// </summary>
        /// <param name="timeoutInMs"></param>
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

            ((XSocketClient) this)._lastPong = DateTime.Now;
            this.OnPong += async (s, m) =>
            {
                //Got a pong back... set time for received pong.
                ((XSocketClient) this)._lastPong = DateTime.Now;

                if (Encoding.UTF8.GetString(m.Blob.ToArray()) != this.PersistentId.ToString())
                {
                    await this.Disconnect();
                }
            };
            //Call ping on interval...

            this._hearbeatTimer = new Timer(intervallMs);
            this._hearbeatTimer.Elapsed += async (s, d) =>
            {
                if (((XSocketClient) this)._lastPong < DateTime.Now.AddMilliseconds(-(intervallMs * 2)))
                {
                    await this.Disconnect();
                    this._hearbeatTimer.Stop();
                    this._hearbeatTimer.Dispose();
                    return;
                }

                await this.Ping(Encoding.UTF8.GetBytes(this.PersistentId.ToString()));
            };
            this._hearbeatTimer.Start();
        }
    }
}