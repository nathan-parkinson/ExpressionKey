﻿using System.Collections.Generic;

namespace ExpressionKey
{
    public interface IEntityPool
    {
        void AddEntities<T>(IEnumerable<T> entities);
        IEnumerable<T> GetEntities<T>(IEnumerable<T> entities);
        IEnumerable<T> GetAllEntities<T>();
    }
}