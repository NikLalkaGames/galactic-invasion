﻿using Godot;

namespace galacticinvasion.Code.Space;

public partial class SpaceScrolling : Node
{
    [Export]
    private Node3D[] _layers;

    [Export]
    private float _scrollSpeed = 40f; // Скорость прокрутки

    [Export]
    private float _resetPosition = 142.0f; // Позиция, на которой слои будут сбрасываться

    private Vector3 _currentPos;

    public override void _Process(double delta)
    {
        foreach (var layer in _layers)
        {
            _currentPos = layer.Position;
            _currentPos.Y -= _scrollSpeed * (float)delta;

            if (_currentPos.Y < -_resetPosition)
            {
                _currentPos.Y += 2 * _resetPosition; // Возвращаем слой в начальную позицию
            }

            layer.Position = _currentPos;
        }
    }
}
