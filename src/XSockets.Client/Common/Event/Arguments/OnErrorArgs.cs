
namespace XSockets.Common.Event.Arguments
{
    using System;

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