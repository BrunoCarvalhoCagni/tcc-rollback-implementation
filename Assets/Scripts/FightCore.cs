using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace tcc{
public class FightCore : MonoBehaviour
{

     public enum RoundStateType
        {
            Stop,
            Intro,
            Fight,
            KO,
            End,
        }

        [SerializeField]
        private float _battleAreaWidth = 10f;
        public float battleAreaWidth { get { return _battleAreaWidth; } }

        [SerializeField]
        private float _battleAreaMaxHeight = 2f;
        public float battleAreaMaxHeight { get { return _battleAreaMaxHeight; } }

        [SerializeField]
        private GameObject roundUI;

        [SerializeField]
        private List<CatData> catDataList = new List<CatData>();

        public bool debugP1Attack = false;
        public bool debugP2Attack = false;
        public bool debugP1Guard = false;
        public bool debugP2Guard = false;

        public bool debugPlayLastRoundInput = false;

        private float timer = 0;
        private uint maxRoundWon = 3;

        public Cat cat1 { get; private set; }
        public Cat cat2 { get; private set; }

        public uint cat1RoundWon { get; private set; }
        public uint cat2RoundWon { get; private set; }

        public List<Cat> cats { get { return _cats; } }
        private List<Cat> _cats = new List<Cat>();

        private float roundStartTime;
        private int frameCount;

        public RoundStateType roundState { get { return _roundState; } }
        private RoundStateType _roundState = RoundStateType.Stop;

        public System.Action<Cat, Vector2, DamageResult> damageHandler;

        private Animator roundUIAnimator;

        private static uint maxRecordingInputFrame = 60 * 60 * 5;
        private InputData[] recordingP1Input = new InputData[maxRecordingInputFrame];
        private InputData[] recordingP2Input = new InputData[maxRecordingInputFrame];
        private uint currentRecordingInputIndex = 0;

        private InputData[] lastRoundP1Input = new InputData[maxRecordingInputFrame];
        private InputData[] lastRoundP2Input = new InputData[maxRecordingInputFrame];
        private uint currentReplayingInputIndex = 0;
        private uint lastRoundMaxRecordingInput = 0;
        private bool isReplayingLastRoundInput = false;

        public bool isDebugPause { get; private set; }

        private float introStateTime = 3f;
        private float koStateTime = 2f;
        private float endStateTime = 3f;
        private float endStateSkippableTime = 1.5f;


        void Awake()
        {
            // Setup dictionary from ScriptableObject data
            catDataList.ForEach((data) => data.setupDictionary());

            cat1 = new Cat();
            cat2 = new Cat();

            _cats.Add(cat1);
            _cats.Add(cat2);

            if(roundUI != null)
            {
                roundUIAnimator = roundUI.GetComponent<Animator>();
            }
        }
        void FixedUpdate()
        {
            
            switch(_roundState)
            {
                case RoundStateType.Stop:

                    ChangeRoundState(RoundStateType.Intro);

                    break;
                case RoundStateType.Intro:

                    UpdateIntroState();

                    timer -= Time.deltaTime;
                    if (timer <= 0f)
                    {
                        ChangeRoundState(RoundStateType.Fight);
                    }

                    if (debugPlayLastRoundInput
                        && !isReplayingLastRoundInput)
                    {
                        StartPlayLastRoundInput();
                    }

                    break;
                case RoundStateType.Fight:

                    if(CheckUpdateDebugPause())
                    {
                        break;
                    }

                    frameCount++;
                    
                    UpdateFightState();

                    var deadCat = _cats.Find((f) => f.isDead);
                    if(deadCat != null)
                    {
                        ChangeRoundState(RoundStateType.KO);
                    }

                    break;
                case RoundStateType.KO:

                    UpdateKOState();
                    timer -= Time.deltaTime;
                    if (timer <= 0f)
                    {
                        ChangeRoundState(RoundStateType.End);
                    }

                    break;
                case RoundStateType.End:

                    UpdateEndState();
                    timer -= Time.deltaTime;
                    if (timer <= 0f
                        || (timer <= endStateSkippableTime && IsKOSkipButtonPressed()))
                    {
                        ChangeRoundState(RoundStateType.Stop);
                    }

                    break;
            }
        }

        void ChangeRoundState(RoundStateType state)
        {
            _roundState = state;
            switch (_roundState)
            {
                case RoundStateType.Stop:

                    if(cat1RoundWon >= maxRoundWon
                        || cat2RoundWon >= maxRoundWon)
                    {
                        GameManager.Instance.LoadTitleScene();
                    }

                    break;
                case RoundStateType.Intro:

                    cat1.SetupBattleStart(catDataList[0], new Vector2(-2f, 0f), true);
                    cat2.SetupBattleStart(catDataList[0], new Vector2(2f, 0f), false);

                    timer = introStateTime;

                    roundUIAnimator.SetTrigger("RoundStart");

                    break;
                case RoundStateType.Fight:

                    roundStartTime = Time.fixedTime;
                    frameCount = -1;

                    currentRecordingInputIndex = 0;
                    
                    break;
                case RoundStateType.KO:

                    timer = koStateTime;

                    CopyLastRoundInput();

                    cat1.ClearInput();
                    cat2.ClearInput();

                    roundUIAnimator.SetTrigger("RoundEnd");

                    break;
                case RoundStateType.End:

                    timer = endStateTime;

                    var deadCat = _cats.FindAll((f) => f.isDead);
                    if (deadCat.Count == 1)
                    {
                        if (deadCat[0] == cat1)
                        {
                            cat2RoundWon++;
                            cat2.RequestWinAction();
                        }
                        else if (deadCat[0] == cat2)
                        {
                            cat1RoundWon++;
                            cat1.RequestWinAction();
                        }
                    }

                    break;
            }
        }

        void UpdateIntroState()
        {
            var p1Input = GetP1InputData();
            var p2Input = GetP2InputData();
            RecordInput(p1Input, p2Input);
            cat1.UpdateInput(p1Input);
            cat2.UpdateInput(p2Input);

            _cats.ForEach((f) => f.IncrementActionFrame());

            _cats.ForEach((f) => f.UpdateIntroAction());
            _cats.ForEach((f) => f.UpdateMovement());
            _cats.ForEach((f) => f.UpdateBoxes());

            UpdatePushCharacterVsCharacter();
            UpdatePushCharacterVsBackground();
        }

        void UpdateFightState()
        {
            var p1Input = GetP1InputData();
            var p2Input = GetP2InputData();
            RecordInput(p1Input, p2Input);
            cat1.UpdateInput(p1Input);
            cat2.UpdateInput(p2Input);

            _cats.ForEach((f) => f.IncrementActionFrame());

            _cats.ForEach((f) => f.UpdateActionRequest());
            _cats.ForEach((f) => f.UpdateMovement());
            _cats.ForEach((f) => f.UpdateBoxes());

            UpdatePushCharacterVsCharacter();
            UpdatePushCharacterVsBackground();
            UpdateHitboxHurtboxCollision();
        }

        void UpdateKOState()
        {

        }
        void UpdateEndState()
        {
            _cats.ForEach((f) => f.IncrementActionFrame());

            _cats.ForEach((f) => f.UpdateActionRequest());
            _cats.ForEach((f) => f.UpdateMovement());
            _cats.ForEach((f) => f.UpdateBoxes());

            UpdatePushCharacterVsCharacter();
            UpdatePushCharacterVsBackground();
        }


        InputData GetP1InputData()
        {
            if(isReplayingLastRoundInput)
            {
                return lastRoundP1Input[currentReplayingInputIndex];
            }

            var time = Time.fixedTime - roundStartTime;

            InputData p1Input = new InputData();
            p1Input.input |= InputManager.Instance.GetButton(InputManager.Command.p1Left) ? (int)InputDefine.Left : 0;
            p1Input.input |= InputManager.Instance.GetButton(InputManager.Command.p1Right) ? (int)InputDefine.Right : 0;
            p1Input.input |= InputManager.Instance.GetButton(InputManager.Command.p1Attack) ? (int)InputDefine.Attack : 0;
            p1Input.time = time;

            if (debugP1Attack)
                p1Input.input |= (int)InputDefine.Attack;
            if (debugP1Guard)
                p1Input.input |= (int)InputDefine.Left;

            return p1Input;
        }

        InputData GetP2InputData()
        {
            if (isReplayingLastRoundInput)
            {
                return lastRoundP2Input[currentReplayingInputIndex];
            }

            var time = Time.fixedTime - roundStartTime;

            InputData p2Input = new InputData();

            p2Input.input |= InputManager.Instance.GetButton(InputManager.Command.p2Left) ? (int)InputDefine.Left : 0;
            p2Input.input |= InputManager.Instance.GetButton(InputManager.Command.p2Right) ? (int)InputDefine.Right : 0;
            p2Input.input |= InputManager.Instance.GetButton(InputManager.Command.p2Attack) ? (int)InputDefine.Attack : 0;

            p2Input.time = time;

            if (debugP2Attack)
                p2Input.input |= (int)InputDefine.Attack;
            if (debugP2Guard)
                p2Input.input |= (int)InputDefine.Right;

            return p2Input;
        }

        private bool IsKOSkipButtonPressed()
        {
            if (InputManager.Instance.GetButton(InputManager.Command.p1Attack))
                return true;

            if (InputManager.Instance.GetButton(InputManager.Command.p2Attack))
                return true;

            return false;
        }

        void UpdatePushCharacterVsCharacter()
        {
            var rect1 = cat1.pushbox.rect;
            var rect2 = cat2.pushbox.rect;

            if (rect1.Overlaps(rect2))
            {
                if (cat1.position.x < cat2.position.x)
                {
                    cat1.ApplyPositionChange((rect1.xMax - rect2.xMin) * -1 / 2, cat1.position.y);
                    cat2.ApplyPositionChange((rect1.xMax - rect2.xMin) * 1 / 2, cat2.position.y);
                }
                else if (cat1.position.x > cat2.position.x)
                {
                    cat1.ApplyPositionChange((rect2.xMax - rect1.xMin) * 1 / 2, cat1.position.y);
                    cat2.ApplyPositionChange((rect2.xMax - rect1.xMin) * -1 / 2, cat1.position.y);
                }
            }
        }

        void UpdatePushCharacterVsBackground()
        {
            var stageMinX = battleAreaWidth * -1 / 2;
            var stageMaxX = battleAreaWidth / 2;

            _cats.ForEach((f) =>
            {
                if (f.pushbox.xMin < stageMinX)
                {
                    f.ApplyPositionChange(stageMinX - f.pushbox.xMin, f.position.y);
                }
                else if (f.pushbox.xMax > stageMaxX)
                {
                    f.ApplyPositionChange(stageMaxX - f.pushbox.xMax, f.position.y);
                }
            });
        }



        void UpdateHitboxHurtboxCollision()
        {
            foreach(var attacker in _cats)
            {
                Vector2 damagePos = Vector2.zero;
                bool isHit = false;
                bool isProximity = false;
                int hitAttackID = 0;

                foreach (var damaged in _cats)
                {
                    if (attacker == damaged)
                        continue;
                    
                    foreach (var hitbox in attacker.hitboxes)
                    {
                        // continue if attack already hit
                        if(!attacker.CanAttackHit(hitbox.attackID))
                        {
                            continue;
                        }

                        foreach (var hurtbox in damaged.hurtboxes)
                        {
                            if (hitbox.Overlaps(hurtbox))
                            {
                                if (hitbox.proximity)
                                {
                                    isProximity = true;
                                }
                                else
                                {
                                    isHit = true;
                                    hitAttackID = hitbox.attackID;
                                    float x1 = Mathf.Min(hitbox.xMax, hurtbox.xMax);
                                    float x2 = Mathf.Max(hitbox.xMin, hurtbox.xMin);
                                    float y1 = Mathf.Min(hitbox.yMax, hurtbox.yMax);
                                    float y2 = Mathf.Max(hitbox.yMin, hurtbox.yMin);
                                    damagePos.x = (x1 + x2) / 2;
                                    damagePos.y = (y1 + y2) / 2;
                                    break;
                                }
                                
                            }
                        }

                        if (isHit)
                            break;
                    }

                    if (isHit)
                    {
                        attacker.NotifyAttackHit(damaged, damagePos);
                        var damageResult = damaged.NotifyDamaged(attacker.getAttackData(hitAttackID), damagePos);

                        var hitStunFrame = attacker.GetHitStunFrame(damageResult, hitAttackID);
                        attacker.SetHitStun(hitStunFrame);
                        damaged.SetHitStun(hitStunFrame);
                        damaged.SetSpriteShakeFrame(hitStunFrame / 3);

                        damageHandler(damaged, damagePos, damageResult);
                    }
                    else if (isProximity)
                    {
                        damaged.NotifyInProximityGuardRange();
                    }
                }


            }
        }


        void RecordInput(InputData p1Input, InputData p2Input)
        {
            if (currentRecordingInputIndex >= maxRecordingInputFrame)
                return;

            recordingP1Input[currentRecordingInputIndex] = p1Input.ShallowCopy();
            recordingP2Input[currentRecordingInputIndex] = p2Input.ShallowCopy();
            currentRecordingInputIndex++;

            if (isReplayingLastRoundInput)
            {
                if (currentReplayingInputIndex < lastRoundMaxRecordingInput)
                    currentReplayingInputIndex++;
            }
        }


        void CopyLastRoundInput()
        {
            for(int i = 0; i < currentRecordingInputIndex; i++)
            {
                lastRoundP1Input[i] = recordingP1Input[i].ShallowCopy();
                lastRoundP2Input[i] = recordingP2Input[i].ShallowCopy();
            }
            lastRoundMaxRecordingInput = currentRecordingInputIndex;
            
            isReplayingLastRoundInput = false;
            currentReplayingInputIndex = 0;
        }


        void StartPlayLastRoundInput()
        {
            isReplayingLastRoundInput = true;
            currentReplayingInputIndex = 0;
        }


        bool CheckUpdateDebugPause()
        {
            if (Input.GetKeyDown(KeyCode.F1))
            {
                isDebugPause = !isDebugPause;
            }

            if (isDebugPause)
            {
                // press f2 during debug pause to 
                if (Input.GetKeyDown(KeyCode.F2))
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }

            return false;
        }


        public int GetFrameAdvantage(bool getP1)
        {
            var p1FrameLeft = cat1.currentActionFrameCount - cat1.currentActionFrame;
            if (cat1.isAlwaysCancelable)
                p1FrameLeft = 0;

            var p2FrameLeft = cat2.currentActionFrameCount - cat2.currentActionFrame;
            if (cat2.isAlwaysCancelable)
                p2FrameLeft = 0;

            if (getP1)
                return p2FrameLeft - p1FrameLeft;
            else
                return p1FrameLeft - p2FrameLeft;
        }



}
}