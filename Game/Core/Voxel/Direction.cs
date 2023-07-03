using Dan200.Core.Math;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dan200.Core.Voxel
{
    internal enum Direction
    {
        North = 0,
        East,
        South,
        West,
        Up,
        Down
    }

    internal static class DirectionExtensions
    {
        private static int[] DIR_TO_X = { 0, 1, 0, -1, 0, 0 };
        private static int[] DIR_TO_Y = { 0, 0, 0, 0, 1, -1 };
        private static int[] DIR_TO_Z = { 1, 0, -1, 0, 0, 0 };
        private static int[] OPPOSITE = { 2, 3, 0, 1, 5, 4 };

        public static Vector3I ToVector(this Direction dir)
        {
            return new Vector3I(
                DIR_TO_X[(int)dir],
                DIR_TO_Y[(int)dir],
                DIR_TO_Z[(int)dir]
            );
        }

        public static Direction Opposite(this Direction dir)
        {
            return (Direction)OPPOSITE[(int)dir];
        }

        public static Side ToSide(this Direction dir, FlatDirection forward)
        {
            switch (dir)
            {
                case Direction.North:
                case Direction.East:
                case Direction.South:
                case Direction.West:
                default:
                    {
                        return ((FlatDirection)dir).ToSide(forward);
                    }
                case Direction.Up:
                    return Side.Top;
                case Direction.Down:
                    return Side.Bottom;
            }
        }
    }
}
