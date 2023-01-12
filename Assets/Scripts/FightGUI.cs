using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace tcc
{
    /// <summary>
    /// Compute the fight area that is going to be on screen and update cat sprites position
    /// Also update the debug display
    /// </summary>
    public class FightGUI : MonoBehaviour
    {
        #region serialize field

        [SerializeField]
        private GameObject _fightCoreGameObject;

        [SerializeField]
        private GameObject cat1ImageObject;

        [SerializeField]
        private GameObject cat2ImageObject;

        [SerializeField]
        private GameObject hitEffectObject1;

        [SerializeField]
        private GameObject hitEffectObject2;

        [SerializeField]
        private float _fightBoxLineWidth = 2f;

        [SerializeField]
        private GUIStyle debugTextStyle;

        [SerializeField]
        private bool drawDebug = false;

        #endregion

        #region private field

        private FightCore fightCore;

        private Vector2 fightAreaTopLeftPoint;
        private Vector2 fightAreaBottomRightPoint;

        private Vector2 fightPointToScreenScale;
        private float centerPoint;

        private RectTransform rectTransform;

        private Image cat1Image;
        private Image cat2Image;

        private Animator hitEffectAnimator1;
        private Animator hitEffectAnimator2;

        #endregion

        private void Awake()
        {
            rectTransform = gameObject.GetComponent<RectTransform>();

            if (_fightCoreGameObject != null)
            {
                fightCore = _fightCoreGameObject.GetComponent<FightCore>();
                fightCore.damageHandler += OnDamageHandler;
            }

            if (cat1ImageObject != null)
                cat1Image = cat1ImageObject.GetComponent<Image>();
            if (cat2ImageObject != null)
                cat2Image = cat2ImageObject.GetComponent<Image>();

            if (hitEffectObject1 != null)
                hitEffectAnimator1 = hitEffectObject1.GetComponent<Animator>();
            if (hitEffectObject2 != null)
                hitEffectAnimator2 = hitEffectObject2.GetComponent<Animator>();
        }

        private void OnDestroy()
        {
            fightCore.damageHandler -= OnDamageHandler;
        }

        private void FixedUpdate()
        {
            if (Input.GetKeyDown(KeyCode.F12))
            {
                drawDebug = !drawDebug;
            }

            CalculateFightArea();
            CalculateFightPointToScreenScale();

            UpdateSprite();
        }

        void OnGUI()
        {
            if (drawDebug)
            {
                fightCore.cats.ForEach((f) => DrawCat(f));
                
                var labelRect = new Rect(Screen.width * 0.4f, Screen.height * 0.95f, Screen.width * 0.2f, Screen.height * 0.05f);
                debugTextStyle.alignment = TextAnchor.UpperCenter;
                GUI.Label(labelRect, "F1=Pause/Resume, F2=Frame Step, F12=Debug Draw", debugTextStyle);

                //DrawBox(new Rect(fightAreaTopLeftPoint.x,
                //    fightAreaTopLeftPoint.y,
                //    fightAreaBottomRightPoint.x - fightAreaTopLeftPoint.x,
                //    fightAreaBottomRightPoint.y - fightAreaTopLeftPoint.y),
                //    Color.gray, true);
            }
        }

        void UpdateSprite()
        {
            if(cat1Image != null)
            {
                var sprite = fightCore.cat1.GetCurrentMotionSprite();
                if(sprite != null)
                    cat1Image.sprite = sprite;
                
                var position = cat1Image.transform.position;
                position.x = TransformHorizontalFightPointToScreen(fightCore.cat1.position.x) + fightCore.cat1.spriteShakePosition;
                cat1Image.transform.position = position;
            }

            if (cat2Image != null)
            {
                var sprite = fightCore.cat2.GetCurrentMotionSprite();
                if (sprite != null)
                    cat2Image.sprite = sprite;

                var position = cat2Image.transform.position;
                position.x = TransformHorizontalFightPointToScreen(fightCore.cat2.position.x) + fightCore.cat2.spriteShakePosition;
                cat2Image.transform.position = position;
                
            }
        }

        void DrawCat(Cat cat)
        {
            var labelRect = new Rect(0, Screen.height * 0.86f, Screen.width * 0.22f, 50);
            if (cat.isFaceRight)
            {
                labelRect.x = Screen.width * 0.01f;
                debugTextStyle.alignment = TextAnchor.UpperLeft;
            }
            else
            {
                labelRect.x = Screen.width * 0.77f;
                debugTextStyle.alignment = TextAnchor.UpperRight;
            }

            GUI.Label(labelRect, cat.position.ToString(), debugTextStyle);

            labelRect.y += Screen.height * 0.03f;
            var frameAdvantage = fightCore.GetFrameAdvantage(cat.isFaceRight);
            var frameAdvText = frameAdvantage > 0 ? "+" + frameAdvantage : frameAdvantage.ToString();
            GUI.Label(labelRect, "Frame: " + cat.currentActionFrame + "/" + cat.currentActionFrameCount 
                + "(" + frameAdvText + ")", debugTextStyle);

            labelRect.y += Screen.height * 0.03f;
            GUI.Label(labelRect, "Stun: " + cat.currentHitStunFrame, debugTextStyle);

            labelRect.y += Screen.height * 0.03f;
            GUI.Label(labelRect, "Action: " + cat.currentActionID + " " + (CommonActionID)cat.currentActionID, debugTextStyle);

            foreach (var hurtbox in cat.hurtboxes)
            {
                DrawFightBox(hurtbox.rect, Color.yellow, true);
            }

            if (cat.pushbox != null)
            {
                DrawFightBox(cat.pushbox.rect, Color.blue, true);
            }

            foreach (var hitbox in cat.hitboxes)
            {
                if(hitbox.proximity)
                    DrawFightBox(hitbox.rect, Color.gray, true);
                else
                    DrawFightBox(hitbox.rect, Color.red, true);
            }
        }

        void DrawFightBox(Rect fightPointRect, Color color, bool isFilled)
        {
            var screenRect = new Rect();
            screenRect.width = fightPointRect.width * fightPointToScreenScale.x;
            screenRect.height = fightPointRect.height * fightPointToScreenScale.y;
            screenRect.x = TransformHorizontalFightPointToScreen(fightPointRect.x) - (screenRect.width / 2);
            screenRect.y = fightAreaBottomRightPoint.y - (fightPointRect.y * fightPointToScreenScale.y) - screenRect.height;

            DrawBox(screenRect, color, isFilled);
        }

        void DrawBox(Rect rect, Color color, bool isFilled)
        {
            float startX = rect.x;
            float startY = rect.y;
            float width = rect.width;
            float height = rect.height;
            float endX = startX + width;
            float endY = startY + height;

            Draw.DrawLine(new Vector2(startX, startY), new Vector2(endX, startY), color, _fightBoxLineWidth);
            Draw.DrawLine(new Vector2(startX, startY), new Vector2(startX, endY), color, _fightBoxLineWidth);
            Draw.DrawLine(new Vector2(endX, endY), new Vector2(endX, startY), color, _fightBoxLineWidth);
            Draw.DrawLine(new Vector2(endX, endY), new Vector2(startX, endY), color, _fightBoxLineWidth);

            if (isFilled)
            {
                Color rectColor = color;
                rectColor.a = 0.25f;
                Draw.DrawRect(new Rect(startX, startY, width, height), rectColor);
            }
        }

        float TransformHorizontalFightPointToScreen(float x)
        {
            return (x * fightPointToScreenScale.x) + centerPoint;
        }

        float TransformVerticalFightPointToScreen(float y)
        {
            return (Screen.height - fightAreaBottomRightPoint.y) + (y * fightPointToScreenScale.y);
        }

        void CalculateFightArea()
        {
            Vector3[] v = new Vector3[4];
            rectTransform.GetWorldCorners(v);
            fightAreaTopLeftPoint = new Vector2(v[1].x, Screen.height - v[1].y);
            fightAreaBottomRightPoint = new Vector2(v[3].x, Screen.height - v[3].y);
        }

        void CalculateFightPointToScreenScale()
        {

            fightPointToScreenScale.x = (fightAreaBottomRightPoint.x - fightAreaTopLeftPoint.x) / fightCore.fightAreaWidth;
            
            fightPointToScreenScale.y = (fightAreaBottomRightPoint.y - fightAreaTopLeftPoint.y) / fightCore.fightAreaMaxHeight;

            centerPoint = (fightAreaBottomRightPoint.x + fightAreaTopLeftPoint.x) / 2;
        }

        private void OnDamageHandler(Cat damagedCat, Vector2 damagedPos, DamageResult damageResult)
        {
            // Set attacker cat to last sibling so that it will get draw last and be on the most front
            if(damagedCat == fightCore.cat1)
            {
                cat2Image.transform.SetAsLastSibling();

                RequestHitEffect(hitEffectAnimator1, damagedPos, damageResult);
            }
            else if(damagedCat == fightCore.cat2)
            {
                cat1Image.transform.SetAsLastSibling();

                RequestHitEffect(hitEffectAnimator2, damagedPos, damageResult);
            }
        }

        private void RequestHitEffect(Animator hitEffectAnimator, Vector2 damagedPos, DamageResult damageResult)
        {
            hitEffectAnimator.SetTrigger("Hit");
            var position = hitEffectAnimator2.transform.position;
            position.x = TransformHorizontalFightPointToScreen(damagedPos.x);
            position.y = TransformVerticalFightPointToScreen(damagedPos.y);
            hitEffectAnimator.transform.position = position;

            if (damageResult == DamageResult.GuardBreak)
                hitEffectAnimator.transform.localScale = new Vector3(5,5,1);
            else if (damageResult == DamageResult.Damage)
                hitEffectAnimator.transform.localScale = new Vector3(2, 2, 1);
            else if (damageResult == DamageResult.Guard)
                hitEffectAnimator.transform.localScale = new Vector3(1, 1, 1);

            hitEffectAnimator.transform.SetAsLastSibling();
        }
    }

}