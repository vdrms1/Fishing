using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FishingFun
{
    public static class MouseMoveCalculations
    {

        private enum moveDirection
        {
            NORTH_EAST = 0,
            NORTH_WEST = 1,
            SOUTH_EAST = 2,
            SOUTH_WEST = 3
        };

        static private int getPointDiff(int x1, int x2)
        {

            if (x1 > x2)
            {
                return x1 - x2;
            }
            else
            {
                return x2 - x1;
            }
        }

        static private moveDirection determineDirection(System.Drawing.Point startPosition, System.Drawing.Point endPosition)
        {
            if (startPosition.X < endPosition.X)
            {
                // EAST
                if (endPosition.Y > startPosition.Y )
                {

                    return moveDirection.SOUTH_EAST;                    
                }
                else
                {
                    return moveDirection.NORTH_EAST;
                }

            }
            else
            {
                // WEST                
                if (endPosition.Y > startPosition.Y)
                {
                    return moveDirection.SOUTH_WEST;
                }
                else
                {                    
                    return moveDirection.NORTH_WEST;
                }

            }
        }

        static public List<System.Drawing.Point> calculateMovePoints(System.Drawing.Point startPosition, System.Drawing.Point endPosition)
        {
            moveDirection move = determineDirection(startPosition, endPosition);
            int widthMoves = getPointDiff(startPosition.X, endPosition.X);
            int heightMoves = getPointDiff(startPosition.Y, endPosition.Y);

            // TODO: This can't be double << need to fix this 
            double xMove = (double)widthMoves / heightMoves;
            double yMove = (double)heightMoves / widthMoves;

            List<System.Drawing.Point> outputList = new List<System.Drawing.Point>();

            int cycles = (int)xMove > (int)yMove ? (int)xMove : (int)yMove;
            
            
            for(int i = 1; i < cycles + 1; i++)
            {
                if (move == moveDirection.NORTH_EAST)
                {
                    outputList.Add(new System.Drawing.Point(startPosition.X + (int)(xMove*i), startPosition.Y - (int)(yMove*i)));
                }
            }


            return outputList;

        }

    }
}
