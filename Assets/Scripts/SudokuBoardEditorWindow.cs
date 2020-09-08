using UnityEngine;
using UnityEditor;

public class SudokuBoardEditorWindow : EditorWindow
{
    [MenuItem("Window/Sudoku Board Editor Window")]
    static void Init()
    {
        SudokuBoardEditorWindow window = (SudokuBoardEditorWindow)EditorWindow.GetWindow(typeof(SudokuBoardEditorWindow));
        window.Show();
    }

    private SudokuBoardDataAsset CurrentSudokuBoardDataAsset;
    private Vector2Int NumberOfAreasInBoard, NumberOfCubesPerArea;
    private int[,] SudokuBoardData;

    void OnGUI()
    {
        EditorGUILayout.BeginVertical(GUI.skin.box);
        CurrentSudokuBoardDataAsset = (SudokuBoardDataAsset)EditorGUILayout.ObjectField("Asset", CurrentSudokuBoardDataAsset, typeof(SudokuBoardDataAsset), true);
        EditorGUILayout.EndVertical();

        RefreshDataParametersFromAsset();
        EditorGUILayout.Space();
        DrawSudokuBoardDataParametersGUI();
        EditorGUILayout.Space();
        DrawSudokuBoardGUI(NumberOfAreasInBoard.x * NumberOfCubesPerArea.x, NumberOfAreasInBoard.y * NumberOfCubesPerArea.y);
        DrawSaveChangesGUI();
    }

    private void RefreshDataParametersFromAsset()
    {
        if (GUI.changed)
        {
            if (CurrentSudokuBoardDataAsset)
            {
                NumberOfAreasInBoard = CurrentSudokuBoardDataAsset.NumberOfAreasInBoard;
                NumberOfCubesPerArea = CurrentSudokuBoardDataAsset.NumberOfCubesPerArea;
                SudokuBoardData = CurrentSudokuBoardDataAsset.GetSudokuBoardNumbers();
            }
        }
    }

    private void DrawSudokuBoardDataParametersGUI()
    {
        EditorGUILayout.BeginVertical(GUI.skin.box);
        if (CurrentSudokuBoardDataAsset)
        {
            EditorGUILayout.LabelField("Sudoku Board Data Parameters");
            NumberOfAreasInBoard = EditorGUILayout.Vector2IntField("# of Areas In Board", NumberOfAreasInBoard);
            NumberOfCubesPerArea = EditorGUILayout.Vector2IntField("# of Cubes Per Area", NumberOfCubesPerArea);

            if (GUILayout.Button("Create New Sudoku Board Data Matrix"))
            {
                CurrentSudokuBoardDataAsset.Initialise(NumberOfAreasInBoard, NumberOfCubesPerArea);
                SudokuBoardData = new int[NumberOfAreasInBoard.x * NumberOfCubesPerArea.x, NumberOfAreasInBoard.y * NumberOfCubesPerArea.y];
            }
        }
        EditorGUILayout.EndVertical();
    }

    private string[] CubeValueDisplayRange = new string[] { "0", "1", "2", "3", "4", "5", "6", "7", "8", "9" };
    private int[] CubeValueRange = new int[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };

    private void DrawSudokuBoardGUI(int boardWidthInCubes, int boardLengthInCubes)
    {
        EditorGUILayout.BeginVertical(GUI.skin.box);
        if (CurrentSudokuBoardDataAsset)
        {
            if (CurrentSudokuBoardDataAsset.SudokuBoardDataRows != null)
            {
                EditorGUILayout.BeginVertical();
                for (int i = 0; i < boardLengthInCubes; i++)
                {
                    EditorGUILayout.BeginHorizontal();
                    for (int j = 0; j < boardWidthInCubes; j++)
                    {
                        SudokuBoardData[i, j] = EditorGUILayout.IntPopup(SudokuBoardData[i, j], CubeValueDisplayRange, CubeValueRange);
                    }
                    EditorGUILayout.EndHorizontal();
                }
                EditorGUILayout.EndVertical();
            }
        }
        EditorGUILayout.EndVertical();
    }

    private void DrawSaveChangesGUI()
    {
        EditorGUILayout.BeginVertical(GUI.skin.box);
        if (CurrentSudokuBoardDataAsset)
        {
            if (GUILayout.Button("Save Changes to Sudoku Board Data Asset"))
            {
                CurrentSudokuBoardDataAsset.SetSudokuBoardData(SudokuBoardData);

                EditorUtility.SetDirty(CurrentSudokuBoardDataAsset);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
        }
        EditorGUILayout.EndVertical();
    }
}
