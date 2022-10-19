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
    int scorePoint = 0;
    int bestPoint = 0;

    bool canMove;

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

        bestPoint = PlayerPrefs.GetInt("best");
        random2Ratio = PlayerPrefs.GetInt("ratio");
    }

	private void Start()
	{
		btn4x4.onClick.AddListener(() => { length = 4; StartGame(); });
        btn5x5.onClick.AddListener(() => { length = 5; StartGame(); });
        btn6x6.onClick.AddListener(() => { length = 6; StartGame(); });
		btnQuit.onClick.AddListener(() => Application.Quit());
        btnRatio.onClick.AddListener(() => { canMove = false; settingPanel.SetActive(true); });
        btnCloseSetting.onClick.AddListener(() => { StartCoroutine(SetCanMove()); settingPanel.SetActive(false); });
        btnToMenu.onClick.AddListener(BackToMenu);
        btnBack.onClick.AddListener(BackToMenu);
        slider.onValueChanged.AddListener(value =>
        {
            random2Ratio = (int)value;
            ratioText.text = $"Random 2: {random2Ratio}%\n Random 4: {100 - random2Ratio}%";
        });
    }

	private void Update()
	{
        if (canMove)
        {
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

        ratioText.text = $"Random 2: {random2Ratio}%\n Random 4: {100 - random2Ratio}%";
        slider.value = random2Ratio;
        result.text = string.Empty;
        resultTrans.text = string.Empty;
        scorePoint = 0;
        score.text = scorePoint.ToString();
        best.text = bestPoint.ToString();

        matrix = new int[length, length];
        positions = new Vector2[length, length];
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

        LayoutRebuilder.ForceRebuildLayoutImmediate(gridLayout.transform as RectTransform);

        for (i = 0; i < transBG.Count; ++i)
        {
            transBG[i].gameObject.SetActive(i < count);
            if (i < count)
            {
                positions[GetRowIndex(i), GetColumnIndex(i)] = transBG[i].localPosition;
            }
        }

        RandomElement();
        StartCoroutine(SetCanMove());
    }

    private void DoMove()
    {
        Vector2 delta = pointerUpPoint - pointerDownPoint;
        if (delta.magnitude < 10f)
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
        for (i = 0; i < count; ++i)
        {
            j = GetRowIndex(i);
            k = GetColumnIndex(i);
        }
        switch (axis)
        {
            case Axis.up:
                Debug.Log("ио");
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
                Debug.Log("об");
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
                Debug.Log("вС");
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
                Debug.Log("ср");
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
            scorePoint += matrix[targetRow, targetColumn];
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

        IsGameOver();
    }

    private void RefreshElemnt(int row, int column)
    {
        elementImageDic[gameElements[row, column]].color = GetColor(matrix[row, column]);
        elementTextDic[gameElements[row, column]].text = matrix[row, column].ToString();
        if (scorePoint == 2048)
        {
            endText.text = "Congrations !!!";
            continueText.text = "Continue";
            btnContinue.onClick.RemoveAllListeners();
            btnContinue.onClick.AddListener(() => { canMove = false; endPanel.SetActive(false); });
            endPanel.SetActive(true);
        }
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
        score.text = scorePoint.ToString();
        if (scorePoint >= bestPoint)
        {
            bestPoint = scorePoint;
            best.text = bestPoint.ToString();
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
                if (matrix[k, left] == value)
                    return false;
            }
            right = k + 1;
            if (right >= 0 && right < length)
            {
                if (matrix[k, right] == value)
                    return false;
            }
        }
        endText.text = "Failed!";
        continueText.text = "Again";
        btnContinue.onClick.RemoveAllListeners();
        btnContinue.onClick.AddListener(() => { canMove = false; StartGame(); });
        endPanel.SetActive(true);
        return true;
    }

    private void OnApplicationQuit()
    {
        PlayerPrefs.SetInt("best", bestPoint);
        PlayerPrefs.SetInt("ratio", random2Ratio);
        PlayerPrefs.Save();
    }
}
