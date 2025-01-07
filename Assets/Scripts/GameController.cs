using UnityEngine;
using System.Collections;
using System.Collections.Generic;
public class GameController : GameBaseController
{
    public static GameController Instance = null;
    public CharacterSet[] characterSets;
    public GridManager gridManager;
    public Cell[,] grid;
    public GameObject playerPrefab;
    public Transform parent;
    public Color[] playersColor;
    public Sprite[] defaultAnswerBox;
    public List<PlayerController> playerControllers = new List<PlayerController>();
    private bool showCells = false;

    protected override void Awake()
    {
        if (Instance == null) Instance = this;
        base.Awake();
    }

    protected override void Start()
    {
        base.Start();
        this.CreateGrids();
    }

    void CreateGrids()
    {
        Sprite gridTexture = LoaderConfig.Instance.gameSetup.gridTexture != null ?
                            SetUI.ConvertTextureToSprite(LoaderConfig.Instance.gameSetup.gridTexture as Texture2D) : null;

        this.grid = gridManager.CreateGrid(gridTexture);
    }

    private IEnumerator InitialQuestion()
    {
        var questionController = QuestionController.Instance;
        if(questionController == null) yield break;
        questionController.nextQuestion();

        yield return new WaitForEndOfFrame();

        if (questionController.currentQuestion.answersChoics != null &&
            questionController.currentQuestion.answersChoics.Length > 0)
        {
            string[] answers = questionController.currentQuestion.answersChoics;
            this.gridManager.UpdateGridWithWord(answers, null);
        }
        else
        {
            string word = questionController.currentQuestion.correctAnswer;
            this.gridManager.UpdateGridWithWord(null, word);
        }
        this.createPlayer();
    }

    void createPlayer()
    {
        var cellPositions = this.gridManager.availablePositions;
        var characterPositionList = this.gridManager.CharacterPositionsCellIds;

        for (int i = 0; i < this.maxPlayers; i++)
        {
            if (i < this.playerNumber)
            {
                var playerController = GameObject.Instantiate(this.playerPrefab, this.parent).GetComponent<PlayerController>();
                playerController.gameObject.name = "Player_" + i;
                playerController.UserId = i;
                this.playerControllers.Add(playerController);
                var cellVector2 = cellPositions[characterPositionList[i]];
                Vector3 actualCellPosition = this.gridManager.cells[cellVector2.x, cellVector2.y].transform.localPosition;
                this.playerControllers[i].Init(this.characterSets[i], this.defaultAnswerBox, actualCellPosition);

                if (i == 0 && LoaderConfig.Instance != null && LoaderConfig.Instance.apiManager.peopleIcon != null)
                {
                    var _playerName = LoaderConfig.Instance?.apiManager.loginName;
                    var icon = SetUI.ConvertTextureToSprite(LoaderConfig.Instance.apiManager.peopleIcon as Texture2D);
                    this.playerControllers[i].UserName = _playerName;
                    this.playerControllers[i].updatePlayerIcon(true, _playerName, icon, this.playersColor[i]);
                }
                else
                {
                    this.playerControllers[i].updatePlayerIcon(true, null, null, this.playersColor[i]);
                }
            }
            else
            {
                int notUsedId = i + 1;
                var notUsedPlayerIcon = GameObject.FindGameObjectWithTag("P" + notUsedId + "_Icon");
                if (notUsedPlayerIcon != null) notUsedPlayerIcon.SetActive(false);

                var notUsedPlayerController = GameObject.FindGameObjectWithTag("P" + notUsedId + "-controller");
                if (notUsedPlayerController != null) notUsedPlayerController.SetActive(false);
            }
        }
    }


    public override void enterGame()
    {
        base.enterGame();
        StartCoroutine(this.InitialQuestion());
    }

    public override void endGame()
    {
        bool showSuccess = false;
        for (int i = 0; i < this.playerControllers.Count; i++)
        {
            if(i < this.playerNumber)
            {
                var playerController = this.playerControllers[i];
                if (playerController != null)
                {
                    if (playerController.Score >= 30)
                    {
                        showSuccess = true;
                    }
                    this.endGamePage.updateFinalScore(i, playerController.Score);
                }
            }
        }
        this.endGamePage.setStatus(true, showSuccess);

        base.endGame();
    }

    public void UpdateNextQuestion()
    {
        LogController.Instance?.debug("Next Question");
        QuestionController.Instance?.nextQuestion();

        if (QuestionController.Instance.currentQuestion.answersChoics != null &&
            QuestionController.Instance.currentQuestion.answersChoics.Length > 0)
        {
            string[] answers = QuestionController.Instance.currentQuestion.answersChoics;
            this.gridManager.UpdateGridWithWord(answers, null);
        }
        else
        {
            string word = QuestionController.Instance.currentQuestion.correctAnswer;
            this.gridManager.UpdateGridWithWord(null, word);
        }

        this.playersResetPosition();
    }

    void playersResetPosition()
    {
        var cellPositions = this.gridManager.availablePositions;
        var characterPositionList = this.gridManager.CharacterPositionsCellIds;

        for (int i = 0; i < this.playerNumber; i++)
        {
            if (this.playerControllers[i] != null)
            {
                var cellVector2 = cellPositions[characterPositionList[i]];
                Vector3 actualCellPosition = this.gridManager.cells[cellVector2.x, cellVector2.y].transform.localPosition;
                this.playerControllers[i].resetRetryTime();
                this.playerControllers[i].playerReset(actualCellPosition);
            }
        }
    }
   
    
    private void Update()
    {
        if(!this.playing) return;

        if(Input.GetKeyDown(KeyCode.F1))
        {
            this.showCells = !this.showCells;
             this.gridManager.setAllCellsStatus(this.showCells);
        }
        else if (Input.GetKeyDown(KeyCode.F2))
        {
            this.playersResetPosition();
        }
        if (Input.GetKeyDown(KeyCode.F3))
        {
            this.UpdateNextQuestion();
        }

        /*if (this.playerControllers.Count == 0) return;

        for (int j = 0; j < SortOrderController.Instance.roads.Length; j++)
        {
            var road = SortOrderController.Instance.roads[j];
            road.showRoadHint(false);
            for (int i = 0; i < this.playerNumber; i++)
            {
                var characterCanvas = this.playerControllers[i].characterCanvas;
                if (characterCanvas.sortingOrder == road.orderLayer && this.showCells)
                {
                    road.showRoadHint(true);
                }
            }  
        }

       // bool anyPlayerReachToSubmitPoint = false;

        for (int i = 0; i < this.playerNumber; i++)
        {
            if (this.playerControllers[i] != null)
            {
                var player = this.playerControllers[i];
                switch (player.stayTrail)
                {
                    case StayTrail.submitPoint:
                        if (player.Retry > 0 && !this.showingPopup)
                        {
                            int currentTime = Mathf.FloorToInt(((this.gameTimer.gameDuration - this.gameTimer.currentTime) / this.gameTimer.gameDuration) * 100);
                            this.playerControllers[i].checkAnswer(currentTime, () =>
                            {
                                for (int i = 0; i < this.playerNumber; i++)
                                {
                                    if (this.playerControllers[i] != null)
                                    {
                                        this.playerControllers[i].playerReset();
                                    }
                                }
                            });
                        }
                        break;
                    case StayTrail.startPoints:
                        player.autoDeductAnswer();
                        break;
                }
            }
        }

        bool isBattleMode = this.playerNumber > 1;

        if (isBattleMode)
        {
            bool isNextQuestion = true;

            for (int i = 0; i < this.playerNumber; i++)
            {
                if (this.playerControllers[i] == null || !this.playerControllers[i].IsTriggerToNextQuestion)
                {
                    isNextQuestion = false;
                    break;
                }
            }

            if (isNextQuestion)
            {
                this.UpdateNextQuestion();
            }
        }
        else
        {
            if (this.playerControllers[0] != null && this.playerControllers[0].IsTriggerToNextQuestion)
            {
                this.UpdateNextQuestion();
            }
        }
        */

    } 
}


public enum CharacterStatus
{
    idling,
    rotating,
    moving,
    getWord,
    recover
}