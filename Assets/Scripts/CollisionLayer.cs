using System;

[Flags]
public enum CollisionLayer
{
    Default = 1 << 0,
    GameWorld = 1 << 3,
    Player = 1 << 6,
    Bullet = 1 << 7,
    Boid = 1 << 8,
}