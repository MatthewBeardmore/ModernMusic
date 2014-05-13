using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI;
using Windows.UI.StartScreen;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;

namespace ModernMusic
{
    public static class SecondaryTileManager
    {
        public static bool TileExists(string appbarTileId)
        {
            return SecondaryTile.Exists(appbarTileId);
        }

        public static async Task<bool> UnpinSecondaryTile(string appbarTileId)
        {
            SecondaryTile secondaryTile = new SecondaryTile(appbarTileId);
            return await secondaryTile.RequestDeleteAsync();
        }

        public static async void PinSecondaryTile(string appbarTileId, string tileName, Uri squareLogo, 
            string tileActivationArguments = "none", bool showName = false)
        {
            SecondaryTile secondaryTile = new SecondaryTile(appbarTileId,
                                                            tileName,
                                                            tileActivationArguments,
                                                            squareLogo,
                                                            TileSize.Square150x150);

            secondaryTile.VisualElements.BackgroundColor = Colors.Transparent;
            secondaryTile.VisualElements.ShowNameOnSquare150x150Logo = showName;

            await secondaryTile.RequestCreateAsync();
        }
    }
}
