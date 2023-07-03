using Dan200.Core.Math;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dan200.Core.Voxel
{
    internal enum FlatDirection
    {
        North = 0,
        East,
        South,
        West
    }

    internal static class FlatDirectionExtensions
    {
        private static int[] DIR_TO_X = { 0, 1, 0, -1 };
        private static int[] DIR_TO_Z = { 1, 0, -1, 0 };
        private static int[] ROTATE_LEFT = { 3, 0, 1, 2 };
        private static int[] ROTATE_RIGHT = { 1, 2, 3, 0 };
        private static int[] OPPOSITE = { 2, 3, 0, 1 };

        public static Vector3I ToVector(this FlatDirection dir)
        {
            return new Vector3I(
                DIR_TO_X[(int)dir],
                0,
                DIR_TO_Z[(int)dir]
            );
        }

        public static Direction ToDirection(this FlatDirection flatDir)
        {
            return (Direction)flatDir;
        }

        public static float ToYaw(this FlatDirection dir)
        {
            return (float)dir * 0.5f * Mathf.PI;
        }

        public static FlatDirection RotateLeft(this FlatDirection dir)
        {
            return (FlatDirection)ROTATE_LEFT[(int)dir];
        }

        public static FlatDirection RotateRight(this FlatDirection dir)
        {
            return (FlatDirection)ROTATE_RIGHT[(int)dir];
        }

        public static FlatDirection Rotate180(this FlatDirection dir)
        {
            return (FlatDirection)OPPOSITE[(int)dir];
        }

        public static FlatDirection Opposite(this FlatDirection dir)
        {
            return (FlatDirection)OPPOSITE[(int)dir];
        }

        public static Side ToSide(this FlatDirection dir, FlatDirection forward)
        {
            switch (forward)
            {
                case FlatDirection.North:
                default:
                    {
                        return (Side)dir;
                    }
                case FlatDirection.East:
                    {
                        return (Side)dir.RotateLeft();
                    }
                case FlatDirection.South:
                    {
                        return (Side)dir.Rotate180();
                    }
                case FlatDirection.West:
                    {
                        return (Side)dir.RotateRight();
                    }
            }
        }
    }
}
