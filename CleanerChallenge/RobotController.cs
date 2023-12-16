using Microsoft.AspNetCore.Mvc;

namespace CleanerChallenge;

[ApiController]
[Route("api")]
public class RobotController : ControllerBase
{
    private static readonly Random _random = new();
    private static readonly List<Point> _directions = new();
    private static readonly bool[,] _visitedMap = new bool[State.Width, State.Height];
    private const bool isFast = true;
    private const int sockPenalty = -146880;

    [HttpGet("reset")]
    public int Reset()
    {
        InitializeDirections(5);

        // Random seed for socks will be changed
        return 123;
    }

    [HttpPost("next")]
    public Command Next(State state)
    {
        Point robotPosition = state.robot;
        Point targetPoint = SelectTargetPoint(state, robotPosition);
        return new Command { x = targetPoint.x, y = targetPoint.y, fast = isFast };
    }

    private static Point SelectTargetPoint(State state, Point p)
    {
        // check all directions and calculate scores
        IOrderedEnumerable<(Point point, int score)> pointScores = _directions.Select(d => CalculatePointScore(state, p, d)).OrderByDescending(ps => ps.score);

        // get maximum unvisited point score
        Point targetPoint = pointScores.Where(ps => !IsVisited(ps.point)).Select(ps => ps.point).FirstOrDefault();

        if (targetPoint.Equals(default(Point)))
        {
            // last hope to unstuck
            targetPoint = GetRandomUnvisitedPoint();
        }

        SetAsVisited(targetPoint);

        return targetPoint;
    }

    private static (Point point, int score) CalculatePointScore(State state, Point start, Point direction)
    {
        byte[,] data = state.Decode2();
        Point point = start;
        int score = 0;

        while (true)
        {
            Point tempPos = new() { x = point.x + direction.x, y = point.y + direction.y };

            if (!IsInArea(tempPos) || IsThereWall(data, tempPos))
            {
                break;
            }

            point = tempPos;
            score += GetDustValue(data, point);

            if (IsThereSock(state, tempPos))
            {
                score += sockPenalty;
            }
        }

        return (point, score);
    }

    private static void SetAsVisited(Point point)
    {
        _visitedMap[point.x, point.y] = true;
    }

    private static bool IsVisited(Point point)
    {
        return _visitedMap[point.x, point.y];
    }

    private static Point GetRandomUnvisitedPoint()
    {
        Point p = new();

        do
        {
            p.x = _random.Next(State.Width);
            p.y = _random.Next(State.Height);
        } while (IsVisited(p));

        return p;
    }

    private static bool IsThereSock(State state, Point p)
    {
        const int R2 = 24 * 24;

        for (int i = 0; i < state.socks.Length; i++)
        {
            int dx = p.x - state.socks[i].x;
            int dy = p.y - state.socks[i].y;
            if ((dx * dx) + (dy * dy) <= R2)
            {
                return true;
            }
        }

        return false;
    }


    private static int GetDustValue(byte[,] data, Point p)
    {
        return 255 - data[p.x, p.y];
    }

    private static bool IsInArea(Point p)
    {
        return p.x > -1 && p.x < State.Width && p.y > -1 && p.y < State.Height;
    }

    private static bool IsThereWall(byte[,] data, Point p)
    {
        int R2 = 32 * 32;

        for (int i = -32; i < 32; i++)
        {
            for (int j = -32; j < 32; j++)
            {
                int x = p.x + i;
                int y = p.y + j;

                if (IsInArea(new Point { x = x, y = y }) && data[x, y] == 0 && (i * i) + (j * j) < R2)
                {
                    return true;
                }
            }
        }
        return false;
    }

    private static void InitializeDirections(int level)
    {
        _directions.Clear();

        for (int i = 0; i <= level; i++)
        {
            switch (i)
            {
                case 0:
                    _directions.AddRange(new Point[]
                    {
                        new() { x = -1, y = 0 },
                        new() { x = 0, y = -1 },
                        new() { x = 0, y = 1 },
                        new() { x = 1, y = 0 },
                    });
                    break;
                case 1:
                    _directions.AddRange(new Point[]
                    {
                        new() { x = -1, y = -1 },
                        new() { x = -1, y = 1 },
                        new() { x = 1, y = -1 },
                        new() { x = 1, y = 1 },
                    });
                    break;
                case 2:
                    _directions.AddRange(new Point[]
                    {
                        new() { x = -2, y = -1 },
                        new() { x = -2, y = 1 },
                        new() { x = -1, y = -2 },
                        new() { x = -1, y = 2 },
                        new() { x = 1, y = -2 },
                        new() { x = 1, y = 2 },
                        new() { x = 2, y = -1 },
                        new() { x = 2, y = 1 },
                    });
                    break;
                case 3:
                    _directions.AddRange(new Point[]
                    {
                        new() { x = -3, y = -2 },
                        new() { x = -3, y = -1 },
                        new() { x = -3, y = 1 },
                        new() { x = -3, y = 2 },
                        new() { x = -2, y = -3 },
                        new() { x = -2, y = 3 },
                        new() { x = -1, y = -3 },
                        new() { x = -1, y = 3 },
                        new() { x = 1, y = -3 },
                        new() { x = 1, y = 3 },
                        new() { x = 2, y = -3 },
                        new() { x = 2, y = 3 },
                        new() { x = 3, y = -2 },
                        new() { x = 3, y = -1 },
                        new() { x = 3, y = 1 },
                        new() { x = 3, y = 2 },
                    });
                    break;
                case 4:
                    _directions.AddRange(new Point[]
                    {
                        new() { x = -4, y = -3 },
                        new() { x = -4, y = -1 },
                        new() { x = -4, y = 1 },
                        new() { x = -4, y = 3 },
                        new() { x = -3, y = -4 },
                        new() { x = -3, y = 4 },
                        new() { x = -1, y = -4 },
                        new() { x = -1, y = 4 },
                        new() { x = 1, y = -4 },
                        new() { x = 1, y = 4 },
                        new() { x = 3, y = -4 },
                        new() { x = 3, y = 4 },
                        new() { x = 4, y = -3 },
                        new() { x = 4, y = -1 },
                        new() { x = 4, y = 1 },
                        new() { x = 4, y = 3 },
                    });
                    break;
                case 5:
                    _directions.AddRange(new Point[]
                    {
                        new() { x = -5, y = -4 },
                        new() { x = -5, y = -3 },
                        new() { x = -5, y = -2 },
                        new() { x = -5, y = -1 },
                        new() { x = -5, y = 1 },
                        new() { x = -5, y = 2 },
                        new() { x = -5, y = 3 },
                        new() { x = -5, y = 4 },
                        new() { x = -4, y = -5 },
                        new() { x = -4, y = 5 },
                        new() { x = -3, y = -5 },
                        new() { x = -3, y = 5 },
                        new() { x = -2, y = -5 },
                        new() { x = -2, y = 5 },
                        new() { x = -1, y = -5 },
                        new() { x = -1, y = 5 },
                        new() { x = 1, y = -5 },
                        new() { x = 1, y = 5 },
                        new() { x = 2, y = -5 },
                        new() { x = 2, y = 5 },
                        new() { x = 3, y = -5 },
                        new() { x = 3, y = 5 },
                        new() { x = 4, y = -5 },
                        new() { x = 4, y = 5 },
                        new() { x = 5, y = -4 },
                        new() { x = 5, y = -3 },
                        new() { x = 5, y = -2 },
                        new() { x = 5, y = -1 },
                        new() { x = 5, y = 1 },
                        new() { x = 5, y = 2 },
                        new() { x = 5, y = 3 },
                        new() { x = 5, y = 4 },
                    });
                    break;
                case 6:
                    _directions.AddRange(new Point[]
                    {
                        new() { x = -6, y = -5 },
                        new() { x = -6, y = -1 },
                        new() { x = -6, y = 1 },
                        new() { x = -6, y = 5 },
                        new() { x = -5, y = -6 },
                        new() { x = -5, y = 6 },
                        new() { x = -1, y = -6 },
                        new() { x = -1, y = 6 },
                        new() { x = 1, y = -6 },
                        new() { x = 1, y = 6 },
                        new() { x = 5, y = -6 },
                        new() { x = 5, y = 6 },
                        new() { x = 6, y = -5 },
                        new() { x = 6, y = -1 },
                        new() { x = 6, y = 1 },
                        new() { x = 6, y = 5 },
                    });
                    break;
                case 7:
                    _directions.AddRange(new Point[]
                    {
                        new() { x = -7, y = -6 },
                        new() { x = -7, y = -5 },
                        new() { x = -7, y = -4 },
                        new() { x = -7, y = -3 },
                        new() { x = -7, y = -2 },
                        new() { x = -7, y = -1 },
                        new() { x = -7, y = 1 },
                        new() { x = -7, y = 2 },
                        new() { x = -7, y = 3 },
                        new() { x = -7, y = 4 },
                        new() { x = -7, y = 5 },
                        new() { x = -7, y = 6 },
                        new() { x = -6, y = -7 },
                        new() { x = -6, y = 7 },
                        new() { x = -5, y = -7 },
                        new() { x = -5, y = 7 },
                        new() { x = -4, y = -7 },
                        new() { x = -4, y = 7 },
                        new() { x = -3, y = -7 },
                        new() { x = -3, y = 7 },
                        new() { x = -2, y = -7 },
                        new() { x = -2, y = 7 },
                        new() { x = -1, y = -7 },
                        new() { x = -1, y = 7 },
                        new() { x = 1, y = -7 },
                        new() { x = 1, y = 7 },
                        new() { x = 2, y = -7 },
                        new() { x = 2, y = 7 },
                        new() { x = 3, y = -7 },
                        new() { x = 3, y = 7 },
                        new() { x = 4, y = -7 },
                        new() { x = 4, y = 7 },
                        new() { x = 5, y = -7 },
                        new() { x = 5, y = 7 },
                        new() { x = 6, y = -7 },
                        new() { x = 6, y = 7 },
                        new() { x = 7, y = -6 },
                        new() { x = 7, y = -5 },
                        new() { x = 7, y = -4 },
                        new() { x = 7, y = -3 },
                        new() { x = 7, y = -2 },
                        new() { x = 7, y = -1 },
                        new() { x = 7, y = 1 },
                        new() { x = 7, y = 2 },
                        new() { x = 7, y = 3 },
                        new() { x = 7, y = 4 },
                        new() { x = 7, y = 5 },
                        new() { x = 7, y = 6 },
                    });
                    break;
                case 8:
                    _directions.AddRange(new Point[]
                    {
                        new() { x = -8, y = -7 },
                        new() { x = -8, y = -5 },
                        new() { x = -8, y = -3 },
                        new() { x = -8, y = -1 },
                        new() { x = -8, y = 1 },
                        new() { x = -8, y = 3 },
                        new() { x = -8, y = 5 },
                        new() { x = -8, y = 7 },
                        new() { x = -7, y = -8 },
                        new() { x = -7, y = 8 },
                        new() { x = -5, y = -8 },
                        new() { x = -5, y = 8 },
                        new() { x = -3, y = -8 },
                        new() { x = -3, y = 8 },
                        new() { x = -1, y = -8 },
                        new() { x = -1, y = 8 },
                        new() { x = 1, y = -8 },
                        new() { x = 1, y = 8 },
                        new() { x = 3, y = -8 },
                        new() { x = 3, y = 8 },
                        new() { x = 5, y = -8 },
                        new() { x = 5, y = 8 },
                        new() { x = 7, y = -8 },
                        new() { x = 7, y = 8 },
                        new() { x = 8, y = -7 },
                        new() { x = 8, y = -5 },
                        new() { x = 8, y = -3 },
                        new() { x = 8, y = -1 },
                        new() { x = 8, y = 1 },
                        new() { x = 8, y = 3 },
                        new() { x = 8, y = 5 },
                        new() { x = 8, y = 7 },
                    });
                    break;
                case 9:
                    _directions.AddRange(new Point[]
                    {
                        new() { x = -9, y = -8 },
                        new() { x = -9, y = -7 },
                        new() { x = -9, y = -5 },
                        new() { x = -9, y = -4 },
                        new() { x = -9, y = -2 },
                        new() { x = -9, y = -1 },
                        new() { x = -9, y = 1 },
                        new() { x = -9, y = 2 },
                        new() { x = -9, y = 4 },
                        new() { x = -9, y = 5 },
                        new() { x = -9, y = 7 },
                        new() { x = -9, y = 8 },
                        new() { x = -8, y = -9 },
                        new() { x = -8, y = 9 },
                        new() { x = -7, y = -9 },
                        new() { x = -7, y = 9 },
                        new() { x = -5, y = -9 },
                        new() { x = -5, y = 9 },
                        new() { x = -4, y = -9 },
                        new() { x = -4, y = 9 },
                        new() { x = -2, y = -9 },
                        new() { x = -2, y = 9 },
                        new() { x = -1, y = -9 },
                        new() { x = -1, y = 9 },
                        new() { x = 1, y = -9 },
                        new() { x = 1, y = 9 },
                        new() { x = 2, y = -9 },
                        new() { x = 2, y = 9 },
                        new() { x = 4, y = -9 },
                        new() { x = 4, y = 9 },
                        new() { x = 5, y = -9 },
                        new() { x = 5, y = 9 },
                        new() { x = 7, y = -9 },
                        new() { x = 7, y = 9 },
                        new() { x = 8, y = -9 },
                        new() { x = 8, y = 9 },
                        new() { x = 9, y = -8 },
                        new() { x = 9, y = -7 },
                        new() { x = 9, y = -5 },
                        new() { x = 9, y = -4 },
                        new() { x = 9, y = -2 },
                        new() { x = 9, y = -1 },
                        new() { x = 9, y = 1 },
                        new() { x = 9, y = 2 },
                        new() { x = 9, y = 4 },
                        new() { x = 9, y = 5 },
                        new() { x = 9, y = 7 },
                        new() { x = 9, y = 8 },
                    });
                    break;
                default:
                    break;
            }
        }
    }

    [HttpPost("restart")]
    public bool Restart(GameOver game)
    {
        Console.WriteLine(game.score);
        return false;
    }
}
