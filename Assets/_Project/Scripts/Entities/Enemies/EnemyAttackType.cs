using System;
using UnityEngine;

[Flags]
public enum EnemyAttackType
{
    None = 0,
    Melee = 1 << 0,
    Ranged = 1 << 1
}
