using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Shapes;

namespace NetSweeper {
    struct Point {
        public int x, y;
        public Point(int x, int y) {
            this.x = x;
            this.y = y;
        }
    }
    class Game {
        public struct Tile {
            public int neighborMineCount;
            public bool isMine;
            public bool isExposed;
        }

        public int gameSize { get; private set; }
        public int turns { get; private set; }
        public bool gameOver { get; private set; }

        public Tile[,] tiles { get; private set; }
        
        public Game(int gameSize = 8, int mineCount = 10, int gameSeed = -1) {
            this.gameSize = gameSize;

            tiles = new Tile[gameSize, gameSize];

            var neighbors = new Point[]{
                new Point(-1, -1),
                new Point( 1, -1),
                new Point(-1,  1),
                new Point( 1,  1),
                new Point( 1,  0),
                new Point(-1,  0),
                new Point( 0,  1),
                new Point( 0, -1),
            };

            // place mines randomly
            Random r = gameSeed >= 0 ? new Random(gameSeed) : new Random();
            int x = r.Next(0, gameSize), y = r.Next(0, gameSize);
            for (int i = 0; i < mineCount; i++) {
                while (tiles[x, y].isMine) { // make sure we pick a spot that isn't already a mine
                    x = r.Next(0, gameSize);
                    y = r.Next(0, gameSize);
                }
                tiles[x, y].isMine = true;
                foreach (Point c in neighbors)
                    if (x + c.x >= 0 && x + c.x < gameSize &&
                        y + c.y >= 0 && y + c.y < gameSize)
                        tiles[x + c.x, y + c.y].neighborMineCount++;
            }

            turns = 0;
            gameOver = false;
        }

        public void PickTile(int x, int y) {
            tiles[x, y].isExposed = true;
            turns++;

            if (tiles[x, y].isMine)
                gameOver = true;

            if (tiles[x, y].neighborMineCount == 0) {
                // flood fill empty tiles
                FloodFillEmpty(x, y);
            }
        }

        void FloodFillEmpty(int x, int y) {
            List<Point> leads = new List<Point>();
            List<Point> nleads = new List<Point>();

            if (FloodCheckTile(x + 1, y)) leads.Add(new Point(x + 1, y));
            if (FloodCheckTile(x - 1, y)) leads.Add(new Point(x - 1, y));
            if (FloodCheckTile(x, y + 1)) leads.Add(new Point(x, y + 1));
            if (FloodCheckTile(x, y - 1)) leads.Add(new Point(x, y - 1));

            while (leads.Count > 0) {
                nleads.Clear();
                foreach (Point p in leads) {
                    if (FloodCheckTile(p.x + 1, p.y)) nleads.Add(new Point(p.x + 1, p.y));
                    if (FloodCheckTile(p.x - 1, p.y)) nleads.Add(new Point(p.x - 1, p.y));
                    if (FloodCheckTile(p.x, p.y + 1)) nleads.Add(new Point(p.x, p.y + 1));
                    if (FloodCheckTile(p.x, p.y - 1)) nleads.Add(new Point(p.x, p.y - 1));
                }
                leads.Clear();
                leads.AddRange(nleads);
            }
        }

        bool FloodCheckTile(int x, int y) {
            if (x >= 0 && x < gameSize && y >= 0 && y < gameSize) {
                if (!tiles[x, y].isExposed) {
                    tiles[x, y].isExposed = true;
                    return tiles[x, y].neighborMineCount == 0; // only search off this tile if it is also empty
                }
            }
            return false;
        }
    }
}
