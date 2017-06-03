using System;

namespace CQRSlite.Domain.Exception
{
    public class ConcurrencyException : System.Exception
    {
        public ConcurrencyException(Guid id, int? expectedVersion = null)
            : base($"A different version than expected was found in aggregate {id}. Expected {expectedVersion}")
        { }
    }
}