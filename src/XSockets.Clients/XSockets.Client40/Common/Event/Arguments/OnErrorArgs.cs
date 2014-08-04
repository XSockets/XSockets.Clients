using System;
using XSockets.Client40.Common.Interfaces;
using XSockets.Client40.Model;

namespace XSockets.Client40.Common.Event.Arguments
{
    public class OnErrorArgs : EventArgs
    {
        public OnErrorArgs(string message)
        {

            Exception = new Exception(message);
        }

        public OnErrorArgs(Exception ex)
        {
            Exception = ex;
        }

        public OnErrorArgs(Exception innerException, string message)
        {
            Exception = new Exception(message, innerException);
        }

        #region Properties

        public Exception Exception { get; private set; }

        #endregion Properties
    }
}