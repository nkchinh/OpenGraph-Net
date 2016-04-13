﻿namespace OpenGraphNet
{
    using System;

    /// <summary>
    /// An invalid specification exception
    /// </summary>
#if !DNXCORE50
    [Serializable]
#endif
    public class InvalidSpecificationException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="InvalidSpecificationException" /> class.
        /// </summary>
        /// <param name="message">The message.</param>
        public InvalidSpecificationException(string message)
            : base(message)
        {
        }
    }
}
