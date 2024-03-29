﻿using UnityEngine;

namespace Sxm.SpatialPartitionStructures.Core.Series
{
    internal interface IDebugDrawer
    {
        void SetColor(Color color);
        void DrawWireCube(Vector3 center, Vector3 size);
    }
}
