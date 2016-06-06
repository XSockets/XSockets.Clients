#if !WINDOWS_UWP && (!WINDOWS_PHONE_APP)

namespace XSockets
{
    using System;
    using System.Linq;
    using System.Text;
    using XSockets.Common.Interfaces;
    using System.Timers;
    public partial class XSocketClient : IXSocketClient
    {
        private Timer HearbeatTimer;

        private void StopHeartbeat()
        {
            if (this.HearbeatTimer != null && this.HearbeatTimer.Enabled)
            {
                this.HearbeatTimer.Stop();
                this.HearbeatTimer.Dispose();
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

            LastPong = DateTime.Now;
            this.OnPong += async (s, m) =>
            {
                //Got a pong back... set time for received pong.
                this.LastPong = DateTime.Now;

                if (Encoding.UTF8.GetString(m.Blob.ToArray()) != this.PersistentId.ToString())
                {
                    await this.Disconnect();
                }
            };
            //Call ping on interval...

            this.HearbeatTimer = new System.Timers.Timer(intervallMs);
            this.HearbeatTimer.Elapsed += async (s, d) =>
            {
                if (LastPong < DateTime.Now.AddMilliseconds(-(intervallMs * 2)))
                {
                    await this.Disconnect();
                    this.HearbeatTimer.Stop();
                    this.HearbeatTimer.Dispose();
                    return;
                }

                await this.Ping(Encoding.UTF8.GetBytes(this.PersistentId.ToString()));
            };
            this.HearbeatTimer.Start();
        }
    }
}
#endif