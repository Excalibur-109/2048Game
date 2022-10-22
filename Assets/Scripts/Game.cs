using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class Game : MonoBehaviour
{
    enum Axis
    {
        none,
        up,
        down,
        left,
        right
    }

    [SerializeField] Button btn4x4;
    [SerializeField] Button btn5x5;
    [SerializeField] Button btn6x6;
    [SerializeField] Button btnQuit;

    [SerializeField] TextMeshProUGUI score;
    [SerializeField] TextMeshProUGUI best;
    [SerializeField] Button btnBack;
    [SerializeField] GridLayoutGroup gridLayout;
	[SerializeField] GameObject prefabBG;

    [SerializeField] GameObject startPanel;
    [SerializeField] GameObject gamePanel;
    [SerializeField] Transform gameSpace;
    [SerializeField] Transform element;
    [SerializeField] Button btnRestart;

    [SerializeField] GameObject settingPanel;
    [SerializeField] Button btnRatio;
    [SerializeField] Slider slider;
    [SerializeField] TextMeshProUGUI ratioText;
    [SerializeField] Button btnCloseSetting;

    [SerializeField] GameObject endPanel;
    [SerializeField] Button btnContinue;
    [SerializeField] Button btnToMenu;
    [SerializeField] TextMeshProUGUI endText;
    [SerializeField] TextMeshProUGUI continueText;

    [SerializeField] TextMeshProUGUI result;
    [SerializeField] TextMeshProUGUI resultTrans;

    [SerializeField]
    [Range(75, 85)]
    int random2Ratio = 80;

    int length;
    List<Transform> transBG;
	List<Transform> transElementPool;

    int[,] matrix;
    Dictionary<int, int[,]> gameMatrix;
    Vector2[,] positions;
    Transform[,] gameElements;
    Dictionary<Transform, TextMeshProUGUI> elementTextDic;
    Dictionary<Transform, Image> elementImageDic;

    int count;
    int row, column;
    List<int> randomLocaltion;

    Axis axis = Axis.none;

    float animTime = 0.25f;
    float scaleMax = 1.2f;
    Dictionary<int, int> scoreDic;
    Dictionary<int, int> bestDic;

    bool canMove;
    bool reach2048;

    Vector2 pointerDownPoint;
    Vector2 pointerUpPoint;

    private void Awake()
	{
		transBG = new List<Transform>();
        transElementPool = new List<Transform>();
        elementTextDic = new Dictionary<Transform, TextMeshProUGUI>();
        elementImageDic = new Dictionary<Transform, Image>();
        randomLocaltion = new List<int>();
        transBG.Add(prefabBG.transform);
        elementImageDic.Add(element, element.GetComponent<Image>());
        elementTextDic.Add(element, element.GetComponentInChildren<TextMeshProUGUI>());
        transElementPool.Add(element);
        element.gameObject.SetActive(false);
        RectTransform gameSpaceRect = (RectTransform)gameSpace;
        startPanel.SetActive(true);
        gamePanel.SetActive(false);
        settingPanel.SetActive(false);
        endPanel.SetActive(false);

        LoadGameData();
    }

	private void Start()
	{
		btn4x4.onClick.AddListener(() => { length = 4; StartGame(); });
        btn5x5.onClick.AddListener(() => { length = 5; StartGame(); });
        btn6x6.onClick.AddListener(() => { length = 6; StartGame(); });
		btnQuit.onClick.AddListener(() =>
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        });
        btnRatio.onClick.AddListener(() => { canMove = false; settingPanel.SetActive(true); });
        btnCloseSetting.onClick.AddListener(() => { StartCoroutine(SetCanMove()); settingPanel.SetActive(false); });
        btnToMenu.onClick.AddListener(BackToMenu);
        btnBack.onClick.AddListener(BackToMenu);
        btnRestart.onClick.AddListener(() =>
        {
            for (int i = 0; i < count; ++i)
            {
                matrix[GetRowIndex(i), GetColumnIndex(i)] = 0;
            }
            scoreDic[length] = 0;
            StartGame();
        });
        slider.onValueChanged.AddListener(value =>
        {
            random2Ratio = (int)value;
            ratioText.text = $"随机 2: {random2Ratio}%\n 随机 4: {100 - random2Ratio}%";
        });
    }

	private void Update()
	{
        if (canMove)
        {
            if (Input.touchCount > 1)
            {
                return;
            }
            if (Input.GetMouseButtonDown(0))
            {
                pointerDownPoint = Input.mousePosition;
            }

            if (Input.GetMouseButtonUp(0))
            {
                pointerUpPoint = Input.mousePosition;
                DoMove();
            }
        }
	}

    private void StartGame()
    {
        startPanel.SetActive(false);
        gamePanel.SetActive(true);
        settingPanel.SetActive(false);
        endPanel.SetActive(false);
        canMove = false;
        ratioText.text = $"随机 2: {random2Ratio}%\n 随机 4: {100 - random2Ratio}%";
        slider.value = random2Ratio;
        result.text = string.Empty;
        resultTrans.text = string.Empty;
        score.text = scoreDic[length].ToString();
        best.text = bestDic[length].ToString();

        reach2048 = false;
        if (gameElements != null)
        {
            for (int j = 0; j < gameElements.GetLength(0); ++j)
            {
                for (int k = 0; k < gameElements.GetLength(1); ++k)
                {
                    if (gameElements[j, k] != null)
                    {
                        EnqueueElement(gameElements[j, k]);
                    }
                }
            }
        }
        gameElements = new Transform[length, length];
        count = length * length;
        int bgStartCtn = transBG.Count;
        int i;
        gridLayout.cellSize = Vector2.one * (960 / length);

        for (i = 0; i < count - bgStartCtn; ++i)
        {
            transBG.Add(Instantiate(prefabBG, gridLayout.transform).transform);
        }
        for (i = 0; i < transBG.Count; i++)
        {
            transBG[i].gameObject.SetActive(i < count);
        }
        LayoutRebuilder.ForceRebuildLayoutImmediate(gridLayout.transform as RectTransform);

        StartCoroutine(StartGameRoutine());
    }

    IEnumerator StartGameRoutine()
    {
        yield return new WaitForSeconds(0.1f);
        int i, j, k;
        positions = new Vector2[length, length];
        for (i = 0; i < count; ++i)
        {
            positions[GetRowIndex(i), GetColumnIndex(i)] = transBG[i].localPosition;
        }
        if (!gameMatrix.ContainsKey(length))
        {
            matrix = new int[length, length];
            gameMatrix.Add(length, matrix);
            RandomElement();
        }
        else
        {
            gameMatrix.TryGetValue(length, out matrix);
            bool allZero = true;
            for (i = 0; i < count; ++i)
            {
                if (matrix[GetRowIndex(i), GetColumnIndex(i)] > 0)
                {
                    allZero = false;
                }
            }
            if (!allZero)
            {
                for (i = 0; i < count; ++i)
                {
                    j = GetRowIndex(i);
                    k = GetColumnIndex(i);
                    if (matrix[j, k] > 0)
                    {
                        Transform trans = GetElement();
                        trans.localPosition = positions[j, k];
                        gameElements[j, k] = trans;
                        RefreshElemnt(j, k);
                        StartCoroutine(AnimShow(trans));
                    }
                }
            }
            else
            {
                RandomElement();
            }
        }

        StartCoroutine(SetCanMove());
    }

    private void DoMove()
    {
        Vector2 delta = pointerUpPoint - pointerDownPoint;
        if (delta.magnitude < 100f)
            return;

        if (Mathf.Abs(delta.x) >= Mathf.Abs(delta.y))
        {
            axis = delta.x >= 0f ? Axis.right : Axis.left;
        }
        else
        {
            axis = delta.y >= 0f ? Axis.up : Axis.down;
        }

        int i, j, k, step;
        bool changed = false;
        switch (axis)
        {
            case Axis.up:
                Debug.Log("上");
                for (i = 0; i < count; ++i)
                { 
                    j = GetRowIndex(i);
                    if (j == 0)
                        continue;
                    k = GetColumnIndex(i);
                    if (matrix[j, k] == 0)
                        continue;
                    for (step = j - 1; step >= 0; --step)
                    {
                        if (matrix[step, k] == 0)
                            continue;
                        if (matrix[step, k] > 0 && matrix[step, k] != matrix[j, k])
                        {
                            ++step;
                            break;
                        }
                        else
                        {
                            break;
                        }
                    }
                    step = step < 0 ? 0 : step;
                    if (j != step)
                    {
                        changed = true;
                        OnMove(j, k, step, k);
                    }
                }
                break;
            case Axis.down:
                Debug.Log("下");
                for (i = count - 1; i >= 0; --i)
                {
                    j = GetRowIndex(i);
                    if (j == length - 1)
                        continue;
                    k = GetColumnIndex(i);
                    if (matrix[j, k] == 0)
                        continue;
                    for (step = j + 1; step < length; ++step)
                    {
                        if (matrix[step, k] == 0)
                            continue;
                        if (matrix[step, k] > 0 && matrix[step, k] != matrix[j, k])
                        {
                            --step;
                            break;
                        }
                        else
                        {
                            break;
                        }
                    }
                    step = step >= length ? length - 1 : step;
                    if (j != step)
                    {
                        changed = true;
                        OnMove(j, k, step, k);
                    }
                }
                break;
            case Axis.left:
                Debug.Log("左");
                for (i = 0; i < count; i++)
                {
                    j = GetRowIndex(i);
                    if (j == 0)
                        continue;
                    k = GetColumnIndex(i);
                    if (matrix[k, j] == 0)
                        continue;
                    for (step = j - 1; step >= 0; --step)
                    {
                        if (matrix[k, step] == 0)
                            continue;
                        if (matrix[k, step] > 0 && matrix[k, step] != matrix[k, j])
                        {
                            ++step;
                            break;
                        }
                        else
                        {
                            break;
                        }
                    }
                    step = step < 0 ? 0 : step;
                    if (j != step)
                    {
                        changed = true;
                        OnMove(k, j, k, step);
                    }
                }
                break;
            case Axis.right:
                Debug.Log("右");
                for (i = count - 1; i >= 0; --i)
                {
                    j = GetRowIndex(i);
                    if (j == length - 1)
                        continue;
                    k = GetColumnIndex(i);
                    if (matrix[k, j] == 0)
                        continue;
                    for (step = j + 1; step < length; ++step)
                    {
                        if (matrix[k, step] == 0)
                            continue;
                        if (matrix[k, step] > 0 && matrix[k, step] != matrix[k, j])
                        {
                            --step;
                            break;
                        }
                        else
                        {
                            break;
                        }
                    }
                    step = step >= length ? length - 1 : step;
                    if (j != step)
                    {
                        changed = true;
                        OnMove(k, j, k, step);
                    }
                }
                break;
        }

        for (i = 0; i < count; i++)
        {
            j = GetRowIndex(i);
            k = GetColumnIndex(i);
            if (matrix[j, k] == -1)
                matrix[j, k] = 0;
        }

        if (changed)
            RandomElement();
    }

    private void OnMove(int originalRow, int originalColumn, int targetRow, int targetColumn)
    {
        int originalValue = matrix[originalRow, originalColumn];
        matrix[originalRow, originalColumn] = 0;
        Transform trans = gameElements[originalRow, originalColumn];
        gameElements[originalRow, originalColumn] = null;
        if (matrix[targetRow, targetColumn] == originalValue)
        {
            scoreDic[length] += matrix[targetRow, targetColumn];
            matrix[targetRow, targetColumn] *= 2;
            switch (axis)
            {
                case Axis.up:
                    matrix[targetRow + 1, targetColumn] = -1;
                    break;
                case Axis.down:
                    matrix[targetRow - 1, targetColumn] = -1;
                    break;
                case Axis.left:
                    matrix[targetRow, targetColumn + 1] = -1;
                    break;
                case Axis.right:
                    matrix[targetRow, targetColumn - 1] = -1;
                    break;
            }
            StartCoroutine(AnimMove(trans, positions[targetRow, targetColumn], true));
            StartCoroutine(AnimCombine(targetRow, targetColumn));
        }
        else
        {
            matrix[targetRow, targetColumn] = originalValue;
            gameElements[targetRow, targetColumn] = trans;
            StartCoroutine(AnimMove(trans, positions[targetRow, targetColumn], false));
        }
    }

    private void RandomElement()
    {
        int index = GetEmptyLocation();
        row = GetRowIndex(index);
        column = GetColumnIndex(index);
        matrix[row, column] = Random24();
        Transform trans = GetElement();
        gameElements[row, column] = trans;
        trans.localPosition = positions[row, column];
        RefreshElemnt(row, column);
        StartCoroutine(AnimShow(trans));

        string str = string.Empty;
        string str2 = string.Empty;
        for (int i = 0; i < count; ++i)
        {
            str += matrix[GetRowIndex(i), GetColumnIndex(i)] + " ";
            str2 += gameElements[GetRowIndex(i), GetColumnIndex(i)] == null ? "N " : "Y ";
            if (i > 0 && i % length == length - 1)
            {
                str += "\n";
                str2 += "\n";
            }
        }
        result.text = str;
        resultTrans.text = str2;

        if (!reach2048)
        {
            for (int i = 0; i < count; ++i)
            {
                if (matrix[GetRowIndex(i), GetColumnIndex(i)] == 2048)
                {
                    reach2048 = true;
                    endText.text = "2048啦 好厉害!!!";
                    continueText.text = "继续";
                    btnContinue.onClick.RemoveAllListeners();
                    btnContinue.onClick.AddListener(() => { StartCoroutine(SetCanMove()); endPanel.SetActive(false); });
                    endPanel.SetActive(true);
                }
            }
        }
        IsGameOver();
    }

    private void RefreshElemnt(int row, int column)
    {
        elementImageDic[gameElements[row, column]].color = GetColor(matrix[row, column]);
        elementTextDic[gameElements[row, column]].text = matrix[row, column].ToString();
    }

    private int Random24()
    {
        return Random.Range(1, 101) > random2Ratio ? 4 : 2;
    }

    private int GetEmptyLocation()
    {
        randomLocaltion.Clear();
        for (int i = 0; i < count; ++i)
        {
            if (matrix[GetRowIndex(i), GetColumnIndex(i)] == 0)
                randomLocaltion.Add(i);
        }
        return randomLocaltion[Random.Range(0, randomLocaltion.Count)];
    }

    private int GetRowIndex(int index)
    {
        return index / length;
    }

    private int GetColumnIndex(int index)
    {
        return index % length;
    }

    private Transform GetElement()
    {
        Transform trans;
        if (transElementPool.Count == 0)
        {
            trans = Instantiate(element, gameSpace);
            elementImageDic.Add(trans, trans.GetComponent<Image>());
            elementTextDic.Add(trans, trans.GetComponentInChildren<TextMeshProUGUI>());
        }
        else
        {
            trans = transElementPool[0];
            transElementPool.RemoveAt(0);
        }
        (trans as RectTransform).SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, gridLayout.cellSize.x);
        (trans as RectTransform).SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, gridLayout.cellSize.y);
        trans.localScale = Vector3.zero;
        trans.gameObject.SetActive(true);
        return trans;
    }

    private void EnqueueElement(Transform element)
    {
        transElementPool.Add(element);
        element.gameObject.SetActive(false);
    }

    IEnumerator AnimShow(Transform trans)
    {
        float timer = 0f;
        float scale = trans.localScale.x;
        while (timer <= animTime)
        {
            timer += Time.unscaledDeltaTime;
            scale = Mathf.Lerp(scale, 1f, timer / animTime);
            trans.localScale = Vector3.one * scale;
            yield return null;
        }
        trans.localScale = Vector3.one;
    }

    IEnumerator AnimMove(Transform trans, Vector2 position, bool enqueue)
    {
        float timer = 0f;
        while (timer <= animTime)
        {
            timer += Time.unscaledDeltaTime;
            trans.localPosition = Vector2.Lerp(trans.localPosition, position, timer / animTime);
            yield return null;
        }
        trans.localPosition = position;
        if (enqueue)
            EnqueueElement(trans);
        string str2 = string.Empty;
        for (int i = 0; i < count; ++i)
        {
            str2 += gameElements[GetRowIndex(i), GetColumnIndex(i)] == null ? "N " : "Y ";
            if (i > 0 && i % length == length - 1)
            {
                str2 += "\n";
            }
        }
        resultTrans.text = str2;
    }

    IEnumerator AnimCombine(int row, int column)
    {
        Transform moveTrans = gameElements[row, column];
        moveTrans.SetAsLastSibling();
        yield return new WaitForSeconds(animTime / 2f);
        float timer = 0f;
        float scale = moveTrans.localScale.x;
        score.text = scoreDic[length].ToString();
        if (scoreDic[length] >= bestDic[length])
        {
            bestDic[length] = scoreDic[length];
            best.text = bestDic[length].ToString();
        }
        RefreshElemnt(row, column);
        while (timer <= animTime)
        {
            timer += Time.unscaledDeltaTime;
            if (timer < animTime / 2f)
                scale = Mathf.Lerp(scale, scaleMax, timer / animTime / 2f);
            else
                scale = Mathf.Lerp(scale, 1f, timer / animTime);
            moveTrans.localScale = Vector3.one * scale;
            yield return null;
        }
        moveTrans.localScale = Vector3.one;
    }

    IEnumerator SetCanMove()
    {
        yield return 0;
        canMove = true;
    }

    private static Color GetColor(int value)
    {
        int r = 1, g = 1, b = 1;
        switch (value)
        {
            case 2:
                r = 129;
                g = 121;
                b = 54;
                break;
            case 4:
                r = 183;
                g = 186;
                b = 107;
                break;
            case 8:
                r = 29;
                g = 149;
                b = 63;
                break;
            case 16:
                r = 222;
                g = 171;
                b = 138;
                break;
            case 32:
                r = 254;
                g = 220;
                b = 189;
                break;
            case 64:
                r = 245;
                g = 130;
                b = 32;
                break;
            case 128:
                r = 252;
                g = 175;
                b = 23;
                break;
            case 256:
                r = 68;
                g = 70;
                b = 147;
                break;
            case 512:
                r = 88;
                g = 94;
                b = 170;
                break;
            case 1024:
                r = 240;
                g = 91;
                b = 114;
                break;
            case 2048:
                r = 237;
                g = 25;
                b = 65;
                break;
            case 4096:
                r = 125;
                g = 88;
                b = 134;
                break;
            case 8192:
                r = 241;
                g = 90;
                b = 34;
                break;
            case 16384:
                r = 64;
                g = 28 ;
                b = 68;
                break;
            default:
                r = 40;
                g = 31;
                b = 29;
                break;
        }
        return new Color(r / 255f, g / 255f, b / 255f);
    }

    private void BackToMenu()
    {
        canMove = false;
        result.text = string.Empty;
        resultTrans.text = string.Empty;
        gamePanel.SetActive(false);
        endPanel.SetActive(false);
        startPanel.SetActive(true);
        SaveGameData();
    }

    private bool IsGameOver()
    {
        int i, j, k;
        for (i = 0; i < count; ++i)
        {
            j = GetRowIndex(i);
            k = GetColumnIndex(i);
            if (matrix[j, k] == 0)
                return false;
        }
        int up, down, left, right;
        int value;
        for (i = 0; i < count; ++i)
        {
            j = GetRowIndex(i);
            k = GetColumnIndex(i);
            value = matrix[j, k];
            up = j - 1;
            if (up >= 0 && up < length)
            {
                if (matrix[up, k] == value)
                    return false;

            }
            down = j + 1;
            if (down >= 0 && down < length)
            {
                if (matrix[down, k] == value)
                    return false;
            }
            left = k - 1;
            if (left >= 0 && left < length)
            {
                if (matrix[j, left] == value)
                    return false;
            }
            right = k + 1;
            if (right >= 0 && right < length)
            {
                if (matrix[j, right] == value)
                    return false;
            }
        }
        endText.text = "结束了哟!!";
        continueText.text = "再来";
        btnContinue.onClick.RemoveAllListeners();
        btnContinue.onClick.AddListener(() => { StartCoroutine(SetCanMove()); StartGame(); });
        endPanel.SetActive(true);
        for (i = 0; i < count; ++i)
        {
            matrix[GetRowIndex(i), GetColumnIndex(i)] = 0;
        }
        scoreDic[length] = 0;
        return true;
    }

    private void LoadGameData()
    {
        random2Ratio = PlayerPrefs.GetInt("ratio");
        if (random2Ratio == 0)
            random2Ratio = 80;

        scoreDic = new Dictionary<int, int>();
        bestDic = new Dictionary<int, int>();
        gameMatrix = new Dictionary<int, int[,]>();
        for (int i = 4; i <= 6; ++i)
        {
            string scoreStr = PlayerPrefs.GetString($"S{i}");
            int.TryParse(scoreStr, out int s);
            scoreDic.Add(i, s); 
            scoreStr = PlayerPrefs.GetString($"B{i}");
            int.TryParse(scoreStr, out s);
            bestDic.Add(i, s);

            bool addToDic = false;
            int[,] temp = new int[i, i];
            string str = PlayerPrefs.GetString(i.ToString());
            if (!string.IsNullOrEmpty(str) && !string.IsNullOrWhiteSpace(str))
            {
                string[] paramsStr = str.Split('|');
                for (int j = 0; j < i * i; ++j)
                {
                    int value = int.Parse(paramsStr[j]);
                    temp[j / i, j % i] = value;
                    if (!addToDic && value > 0)
                    {
                        addToDic = true;
                    }
                }
                if (addToDic)
                {
                    gameMatrix.Add(i, temp);
                }
            }
        }
    }

    private void SaveGameData()
    {
        PlayerPrefs.SetInt("ratio", random2Ratio);
        foreach (var item in scoreDic)
        {
            PlayerPrefs.SetString($"S{item.Key}", item.Value.ToString());
        }
        foreach (var item in bestDic)
        {
            PlayerPrefs.SetString($"B{item.Key}", item.Value.ToString());
        }
        string str;
        foreach (var item in gameMatrix)
        {
            str = string.Empty;
            for (int i = 0; i < item.Value.GetLength(0); ++i)
            {
                for (int j = 0; j < item.Value.GetLength(1); ++j)
                {
                    str += item.Value[i, j].ToString();
                    if (i * j != Mathf.Pow(item.Key - 1, 2))
                    {
                        str += "|";
                    }
                }
            }
            PlayerPrefs.SetString(item.Key.ToString(), str);
        }
        PlayerPrefs.Save();
    }

    private void OnApplicationQuit()
    {
        SaveGameData();
    }
}
