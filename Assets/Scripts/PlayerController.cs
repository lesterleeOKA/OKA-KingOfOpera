﻿using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class PlayerController : UserData
{
    public CharacterMoveController moveButton;
    public BloodController bloodController;
    public CharacterStatus characterStatus = CharacterStatus.idling;
    public Scoring scoring;
    public string answer = string.Empty;
    public bool IsCorrect = false;
    public bool IsTriggerToNextQuestion = false;
    public bool IsCheckedAnswer = false;
    public CanvasGroup answerBoxCg;
    public Image answerBoxFrame;
    public float speed;
    [HideInInspector]
    public Transform characterTransform;
    [HideInInspector]
    public Canvas characterCanvas = null;
    public Vector3 startPosition = Vector3.zero;
    public int characterOrder = 11;
    private CharacterAnimation characterAnimation = null;
    private TextMeshProUGUI answerBox = null;
    public List<Cell> collectedCell = new List<Cell>();
    public float countGetAnswerAtStartPoints = 2f;
    private float countAtStartPoints = 0f;

    private RectTransform rectTransform = null;
    public float rotationSpeed = 200f; // Speed of rotation
    public float moveSpeed = 5f; // Speed of movement
    private Rigidbody2D rb = null;
    public bool isRotating = true;
    private float randomDirection;
    private Vector2 moveDirection;

    public void Init(CharacterSet characterSet = null, Sprite[] defaultAnswerBoxes = null, Vector3 startPos = default)
    {
        this.rb = GetComponent<Rigidbody2D>();
        this.SetRandomRotationDirection();

        this.countAtStartPoints = this.countGetAnswerAtStartPoints;
        this.updateRetryTimes(false);
        this.startPosition = startPos;
        this.characterTransform = this.transform;
        this.characterTransform.localPosition = this.startPosition;
        this.characterCanvas = this.GetComponent<Canvas>();
        this.characterCanvas.sortingOrder = this.characterOrder;
        this.characterAnimation = this.GetComponent<CharacterAnimation>();
        this.characterAnimation.characterSet = characterSet;

        if(this.answerBoxCg != null ) {
            SetUI.Set(this.answerBoxCg, false);
            this.answerBox = this.answerBoxCg.GetComponentInChildren<TextMeshProUGUI>();
        }

        if (this.moveButton == null)
        {
            this.moveButton = GameObject.FindGameObjectWithTag("P" + this.RealUserId + "-controller").GetComponent<CharacterMoveController>();
            this.moveButton.OnPointerClickEvent += this.StopRotation;
        }

        if (this.bloodController == null)
        {
            this.bloodController = GameObject.FindGameObjectWithTag("P" + this.RealUserId + "_Blood").GetComponent<BloodController>();
        }

        if (this.PlayerIcons[0] == null)
        {
            this.PlayerIcons[0] = GameObject.FindGameObjectWithTag("P" + this.RealUserId + "_Icon").GetComponent<PlayerIcon>();
        }

        if (this.scoring.scoreTxt == null)
        {
            this.scoring.scoreTxt = GameObject.FindGameObjectWithTag("P" + this.RealUserId + "_Score").GetComponent<TextMeshProUGUI>();
        }

        if (this.scoring.resultScoreTxt == null)
        {
            this.scoring.resultScoreTxt = GameObject.FindGameObjectWithTag("P" + this.RealUserId + "_ResultScore").GetComponent<TextMeshProUGUI>();
        }

        this.scoring.init();
    }

    void updateRetryTimes(bool deduct = false)
    {
        if (deduct)
        {
            if (this.Retry > 0)
            {
                this.Retry--;
            }

            if (this.bloodController != null)
            {
                this.bloodController.setBloods(false);
            }
        }
        else
        {
            this.NumberOfRetry = LoaderConfig.Instance.gameSetup.retry_times;
            this.Retry = this.NumberOfRetry;
        }
    }

    public void updatePlayerIcon(bool _status = false, string _playerName = "", Sprite _icon = null, Color32 _color = default)
    {
        for (int i = 0; i < this.PlayerIcons.Length; i++)
        {
            if (this.PlayerIcons[i] != null)
            {
                this.PlayerColor = _color;
                this.PlayerIcons[i].playerColor = _color;
                //this.joystick.handle.GetComponent<Image>().color = _color;
                this.PlayerIcons[i].SetStatus(_status, _playerName, _icon);
            }
        }

    }


    string CapitalizeFirstLetter(string str)
    {
        if (string.IsNullOrEmpty(str)) return str; // Return if the string is empty or null
        return char.ToUpper(str[0]) + str.Substring(1).ToLower();
    }

    public void checkAnswer(int currentTime, Action onCompleted = null)
    {
        if (!this.IsCheckedAnswer)
        {
            this.IsCheckedAnswer = true;
            var loader = LoaderConfig.Instance;
            var currentQuestion = QuestionController.Instance?.currentQuestion;
            int eachQAScore = currentQuestion.qa.score.full == 0 ? 10 : currentQuestion.qa.score.full;
            int currentScore = this.Score;
            this.answer = this.answerBox.text.ToLower();
            var lowerQIDAns = currentQuestion.correctAnswer.ToLower();
            int resultScore = this.scoring.score(this.answer, currentScore, lowerQIDAns, eachQAScore);
            this.Score = resultScore;
            this.IsCorrect = this.scoring.correct;
            StartCoroutine(this.showAnswerResult(this.scoring.correct,()=>
            {
                if (this.UserId == 0 && loader != null && loader.apiManager.IsLogined) // For first player
                {
                    float currentQAPercent = 0f;
                    int correctId = 0;
                    float score = 0f;
                    float answeredPercentage;
                    int progress = (int)((float)currentQuestion.answeredQuestion / QuestionManager.Instance.totalItems * 100);

                    if (this.answer == lowerQIDAns)
                    {
                        if (this.CorrectedAnswerNumber < QuestionManager.Instance.totalItems)
                            this.CorrectedAnswerNumber += 1;

                        correctId = 2;
                        score = eachQAScore; // load from question settings score of each question

                        LogController.Instance?.debug("Each QA Score!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!" + eachQAScore + "______answer" + this.answer);
                        currentQAPercent = 100f;
                    }
                    else
                    {
                        if (this.CorrectedAnswerNumber > 0)
                        {
                            this.CorrectedAnswerNumber -= 1;
                        }
                    }

                    if (this.CorrectedAnswerNumber < QuestionManager.Instance.totalItems)
                    {
                        answeredPercentage = this.AnsweredPercentage(QuestionManager.Instance.totalItems);
                    }
                    else
                    {
                        answeredPercentage = 100f;
                    }

                    loader.SubmitAnswer(
                               currentTime,
                               this.Score,
                               answeredPercentage,
                               progress,
                               correctId,
                               currentTime,
                               currentQuestion.qa.qid,
                               currentQuestion.correctAnswerId,
                               this.CapitalizeFirstLetter(this.answer),
                               currentQuestion.correctAnswer,
                               score,
                               currentQAPercent,
                               onCompleted
                               );
                }
                else
                {
                   onCompleted?.Invoke();
                }
            }));
        }
    }

    public void resetRetryTime()
    {
        this.updateRetryTimes(false);
        this.bloodController.setBloods(true);
        this.IsTriggerToNextQuestion = false;
    }

    public IEnumerator showAnswerResult(bool correct, Action onCompleted = null)
    {
        float delay = 2f;
        if (correct)
        {
            LogController.Instance?.debug("Add marks" + this.Score);
            GameController.Instance?.setGetScorePopup(true);
            AudioController.Instance?.PlayAudio(1);
            yield return new WaitForSeconds(delay);
            GameController.Instance?.setGetScorePopup(false);
            GameController.Instance?.UpdateNextQuestion();
        }
        else
        {
            GameController.Instance?.setWrongPopup(true);
            AudioController.Instance?.PlayAudio(2);
            this.updateRetryTimes(true);
            yield return new WaitForSeconds(delay);
            GameController.Instance?.setWrongPopup(false);
            if (this.Retry <= 0)
            {
                this.IsTriggerToNextQuestion = true;
            }
        }
        this.scoring.correct = false;

        onCompleted?.Invoke();
    }

    public void characterReset(Vector3 newStartPostion = default)
    {
        this.randomDirection = UnityEngine.Random.Range(0, 2) == 0 ? 1f : -1f;
        this.startPosition = newStartPostion;
        this.characterCanvas.sortingOrder = this.characterOrder;
        this.characterTransform.localPosition = this.startPosition;
        this.collectedCell.Clear();
    }

    void FixedUpdate()
    {
        if (this.isRotating && this.rectTransform != null) { 
            this.characterStatus = CharacterStatus.rotating;
            Vector3 direction = Vector3.forward * rotationSpeed * Time.deltaTime * randomDirection;
            this.rectTransform.Rotate(direction); 
        }
        if (Input.GetKeyDown(KeyCode.Space) && this.UserId == 0) { 
            this.isRotating = false; 
            this.moveDirection = this.rectTransform.up;
        }
        else if (Input.GetKeyDown(KeyCode.F2))
        {
            var gridManager = GameController.Instance.gridManager;
            this.playerReset(gridManager.newCharacterPosition);
        }

        if (!this.isRotating)
        {
            this.MoveForward();
        }
    }

    void SetRandomRotationDirection()
    {
        this.rectTransform = this.GetComponent<RectTransform>();
        this.rb = GetComponent<Rigidbody2D>();
        this.rb.gravityScale = 0;
        this.randomDirection =  UnityEngine.Random.Range(0, 2) == 0 ? 1f : -1f;
    }

    public void StopRotation(BaseEventData data)
    {
        this.isRotating = false;
        this.moveDirection = this.rectTransform.up;
        this.characterStatus = CharacterStatus.moving;

    }

    void MoveForward()
    {
         /*Vector2 targetPosition = rectTransform.anchoredPosition + (moveDirection * moveSpeed * 1000); 
         rectTransform.DOAnchorPos(targetPosition, 1000f).SetEase(Ease.Linear).SetSpeedBased(true);*/

        this.rb.velocity = this.moveDirection * moveSpeed;
        this.FaceDirection(this.moveDirection);
    }

    private void FaceDirection(Vector2 direction)
    {
        // Make sure the character visually faces the direction of movement
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        this.rb.angularVelocity = 0f;
        transform.rotation = Quaternion.Euler(0, 0, angle - 90); // Subtract 90 to align "up" with the forward direction
    }

    public void playerReset(Vector3 newStartPostion = default)
    {
        this.rb.velocity = Vector2.zero;
        this.rb.angularVelocity = 0f;
        this.isRotating = true;
        this.deductAnswer();
        this.setAnswer("");
        this.characterReset(newStartPostion);
        this.IsCheckedAnswer = false;
        this.IsCorrect = false;
    }

    public void setAnswer(string content)
    {
        if (string.IsNullOrEmpty(content))
        {
            this.answer = "";
            SetUI.Set(this.answerBoxCg, false);
        }
        else
        {
            if(content.Length > 1) { 
                this.answer = content;
            }
            else
            {
                this.answer += content;
            }
            SetUI.Set(this.answerBoxCg, true);
        }

        if(this.answerBox != null)
            this.answerBox.text = this.answer;
    }

    public void autoDeductAnswer()
    {
        if(this.collectedCell.Count > 0) {
            if (this.countAtStartPoints > 0f)
            {
                this.countAtStartPoints -= Time.deltaTime;
            }
            else
            {
                this.deductAnswer();
                this.countAtStartPoints = this.countGetAnswerAtStartPoints;
            }
        }
        else
        {
            this.countAtStartPoints = this.countGetAnswerAtStartPoints;
        }
    }

    public void deductAnswer()
    {
       var gridManager = GameController.Instance.gridManager;
        if (this.answer.Length > 0)
        {
            string deductedChar;
            if (gridManager.isMCType)
            {
                deductedChar = this.answer;
                this.setAnswer("");
            }
            else
            {
                deductedChar = this.answer[this.answer.Length - 1].ToString();
                this.answer = this.answer.Substring(0, this.answer.Length - 1);
                if (this.answerBox != null)
                    this.answerBox.text = this.answer;

                if (this.answer.Length == 0)
                {
                    SetUI.Set(this.answerBoxCg, false);
                }
            }

            if (this.collectedCell.Count > 0)
            {
                var latestCell= this.collectedCell[this.collectedCell.Count - 1];
                latestCell.SetTextStatus(true);
                this.collectedCell.RemoveAt(this.collectedCell.Count - 1);
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Check if the other collider has a specific tag, e.g., "Player"
        if (other.CompareTag("Word"))
        {
            var cell = other.GetComponent<Cell>();
            if (cell != null)
            {
                if (cell.isSelected && this.Retry > 0)
                {
                    LogController.Instance.debug("Player has entered the trigger!" + other.name);
                    AudioController.Instance?.PlayAudio(9);

                    var gridManager = GameController.Instance.gridManager;
                    if (gridManager.isMCType){
                        if (this.collectedCell.Count > 0)
                        {
                            var latestCell = this.collectedCell[this.collectedCell.Count - 1];
                            latestCell.SetTextStatus(true);
                            this.collectedCell.RemoveAt(this.collectedCell.Count - 1);
                        }
                    }
                    this.characterStatus = CharacterStatus.getWord;
                    this.setAnswer(cell.content.text);
                    this.collectedCell.Add(cell);
                    cell.SetTextStatus(false);
                    this.rb.velocity = Vector2.zero;
                    this.rb.angularVelocity = 0f;
                    this.isRotating = true;
                }
            }
        }
        else if (other.CompareTag("Wall"))
        {
            AudioController.Instance?.PlayAudio(8);
            this.deductAnswer();
            StartCoroutine(this.delayResetCharacter());
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Word"))
        {
            var cell = other.GetComponent<Cell>();
            if (cell != null)
            {
                if (cell.isSelected)
                {
                    LogController.Instance.debug("Player has exited the trigger!" + other.name);
                }
            }
        }
    }

    IEnumerator delayResetCharacter()
    {
        if (this.characterStatus != CharacterStatus.recover)
        {
            this.characterStatus = CharacterStatus.recover;
            yield return new WaitForSeconds(2.0f);
            var gridManager = GameController.Instance.gridManager;
            this.playerReset(gridManager.newCharacterPosition);
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if(this.characterStatus == CharacterStatus.moving)
        {
            //Debug.Log("current" + this.rb.velocity.sqrMagnitude);
            //Debug.Log("collision " + collision.rigidbody.velocity.sqrMagnitude);
            collision.rigidbody.velocity += new Vector2(0.05f, 0.05f);
            this.rb.velocity = Vector2.zero;
            this.rb.angularVelocity = 0f;
            this.isRotating = true;
        }
    }
}
