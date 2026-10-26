using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class GameManager : MonoBehaviour
{
    private AsyncOperationHandle<GameObject> tilePrefabHandle;
    private AsyncOperationHandle<IList<Tile>> tileVariationsHandle;
    private GameObject tilePrefab;
    private List<Tile> tileVariations;
    private bool isSetUp = false;

    private void Awake()
    {
        RetrieveAddressableDependencies();
    }

    private async void RetrieveAddressableDependencies()
    {
        tilePrefabHandle = Addressables.LoadAssetAsync<GameObject>(Constants.AddressablePrefabs.Tile);
        tileVariationsHandle = Addressables.LoadAssetsAsync<Tile>(Constants.AddressableLabels.Tile);
        tilePrefab = await tilePrefabHandle.Task;
        tileVariations = (await tileVariationsHandle.Task).ToList();
        isSetUp = true;
    }

    private void OnDestroy()
    {
        if(tilePrefabHandle.IsValid()) Addressables.Release(tilePrefabHandle);
        if(tileVariationsHandle.IsValid()) Addressables.Release(tileVariationsHandle);
    }

    //TODO: a small menu system (unity input system)
    //       - Title Screen (with title) -> PLAY, SETTINGS, EXIT
    //       - Grid Size Picker (after PLAY button) -> a grid with the max size where the player has to drag with his fingers to draw the size they want
    //           - holding and dragging begins activating grid tiles, letting go (unless it's too small of a selection) starts the game
    //       - Pause Menu -> CONTINUE, MAIN MENU

    public void GenerateMemoryGrid(int size)
    {
        if(!isSetUp)
        {
            Debug.LogError("Grid generation was called, but the GameManager was not set up yet. Aborting.");
        }

        int pairCount = size / 2;
        int totalTiles = size * size;

        if(size <= 0 || totalTiles % 2 != 0)
        {
            Debug.LogError("With the received size of: " + size + ", we cannot create a grid with an even number of nodes.");
            return;
        }

        if(pairCount > tileVariations.Count)
        {
            Debug.LogError("Cannot create the requested " + pairCount + " because there are only " + tileVariations.Count + " tile variations");
            return;
        }

        //TODO: generate the tile grid with pairs
    }

    //TODO: selection logic (unity input system)
    //       - clicing on a tile flips it in place to show the logo underneath
    //       - if two become selected
    //           - CORRECT   -> an animation plays, the tiles spin wildly and come close to the camera. by the time they get close, on their backside, there is
    //                          a smaller logo and 2 halves of a description. their cosest sides become jagged and they smash together, creating one text from
    //                          the two description halves.
    //           - INCORRECT -> the selected tiles simply shake a bit while turning red-ish, then flip back face down
    //       - when a choice was made and an animation is playing, we have to stop player input (give a little shake on the device if they try to press smth?)
    //       - when the correct choice is made and the description becomes visible, there needs to be a continue text on screen, and a tap anywhere moves
    //         the game along
    //       - after a correct choice is confirmed and moved along, the correctly selected tiles return to their place, but now have their logos facing up

    //TODO: if all pairs are found and confirmed, the game is over and we return to the main menu
    //       - until this point, the title could be "We Know... Do You?", but once the player succeeds and return to the main menu, momentarily, the title says
    //         "We Know... And Now You Do Too!", before it turns back after a while.

    //TODO: after a certain amount of seconds, if a game is ongoing and there was no input, it automatically resets to the main menu

    //TODO: leaderboard system if we have time and willpower
}
