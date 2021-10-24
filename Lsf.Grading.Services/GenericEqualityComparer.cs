using System;
using System.Collections.Generic;

namespace Lsf.Grading.Services
{
    public class GenericEqualityComparer<T> : IEqualityComparer<T>
    {
        private readonly Func<T?, T?, bool> _equals;
        private readonly Func<T, int> _getHashCode;

        public GenericEqualityComparer(Func<T?, T?, bool> equals)
            : this(equals, x => x?.GetHashCode() ?? -1)
        {
        }

        public GenericEqualityComparer(Func<T?, T?, bool> equals, Func<T, int> getHashCode)
        {
            _getHashCode = getHashCode;
            _equals = equals;
        }

        public bool Equals(T? x, T? y)
        {
            return _equals(x, y);
        }

        public int GetHashCode(T obj)
        {
            return _getHashCode(obj);
        }
    }
}