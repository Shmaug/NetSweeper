using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace NetSweeper {
    public partial class MainWindow : Window {
        SolidColorBrush redBrush = new SolidColorBrush(Color.FromRgb(255, 0, 0));
        SolidColorBrush blackBrush = new SolidColorBrush(Color.FromRgb(0, 0, 0));
        SolidColorBrush grayBrush = new SolidColorBrush(Color.FromRgb(127, 127, 127));
        SolidColorBrush whiteBrush = new SolidColorBrush(Color.FromRgb(255, 255, 255));

        Label[,] labelGrid;
        GameController controller;

        public MainWindow() {
            InitializeComponent();

            controller = new GameController(new Game());

            gameGrid.Columns = controller.game.gameSize;
            gameGrid.Rows = controller.game.gameSize;
            labelGrid = new Label[controller.game.gameSize, controller.game.gameSize];
            double bw = gameGrid.Width / controller.game.gameSize;
            double bh = gameGrid.Height / controller.game.gameSize;
            for (int x = 0; x < controller.game.gameSize; x++)
                for (int y = 0; y < controller.game.gameSize; y++) {
                    labelGrid[x, y] = new Label();
                    labelGrid[x, y].Width = bw;
                    labelGrid[x, y].Height = bh;
                    labelGrid[x, y].Tag = new Point(x, y);
                    labelGrid[x, y].MouseDown += Label_MouseDown;
                    labelGrid[x, y].BorderBrush = blackBrush;
                    labelGrid[x, y].BorderThickness = new Thickness(.5, .5, .5, .5);
                    labelGrid[x, y].Background = grayBrush;
                    
                    gameGrid.Children.Add(labelGrid[x, y]);
                }
        }

        private void Label_MouseDown(object sender, MouseButtonEventArgs e) {
            if (sender is Label) {
                Label l = sender as Label;
                if (l.Tag is Point) {
                    controller.game.PickTile((l.Tag as Point?).Value.x, (l.Tag as Point?).Value.y);
                    UpdateGame();
                }
            }
        }

        void UpdateGame() {
            for (int x = 0; x < controller.game.gameSize; x++)
                for (int y = 0; y < controller.game.gameSize; y++) {
                    if (controller.game.tiles[x, y].isExposed) {
                        labelGrid[x, y].Background = controller.game.tiles[x, y].isMine ? redBrush : whiteBrush;
                        labelGrid[x, y].Content =
                            controller.game.tiles[x, y].isMine ? "X" :
                            (controller.game.tiles[x, y].neighborMineCount > 0 ? controller.game.tiles[x, y].neighborMineCount.ToString() : "");
                    } else {
                        labelGrid[x, y].Background = grayBrush;
                    }
                }
        }
    }
}
