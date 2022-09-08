﻿namespace RustyDTO.Supports;

public enum HexMask
{
    None = 0,
    Center = 1 << 0,
    LeftTop = 1 << 1,
    Top = 1 << 2,
    RigthTop = 1 << 3,
    Rigth = 1 << 4,
    LeftDown = 1 << 5,
    Down = 1 << 6,
    RightDown = 1 << 7,
    All = int.MaxValue
}

public enum HexPosition
{
    None = 0,
    Center = 1,
    LeftTop = 2,
    Top = 3,
    RigthTop = 4,
    Rigth = 5,
    LeftDown = 6,
    Down = 7,
    RightDown = 8,
}