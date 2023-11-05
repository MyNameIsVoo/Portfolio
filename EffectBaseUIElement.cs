using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UTILITA.UI;
using DG.Tweening;

namespace UTILITA
{
    namespace EFFECTS
    {
        public class EffectBaseUIElement : MonoBehaviour
        {
            #region Attributes

            [Header("ATTRIBUTES")]
            [SerializeField] protected bool activateOnEnable = true;
            [SerializeField] protected bool deactivateOnDisable = true;
            [SerializeField] protected bool ignoreActivity;

            [Header("REFERENCES")]
            [SerializeField] protected RectTransform objectRT;
            [SerializeField] protected Image image;
            [SerializeField] protected TextMeshProUGUI text;

            public bool isActive { get; private set; }

            #endregion

            public virtual void Awake()
            {
                Init();

                if (activateOnEnable) 
                    SetActiveEffect(false, true);
            }

            public virtual void OnEnable()
            {
                if (activateOnEnable)
                    SetActiveEffect(true);
            }

            public virtual void OnDisable()
            {
                if (deactivateOnDisable && !VOO.Main.onAppQuit)
                    SetActiveEffect(false, true);
            }

            private void OnValidate()
            {
#if UNITY_EDITOR
                objectRT = GetComponent<RectTransform>();

                if (objectRT != null)
                {
                    image = objectRT.GetComponent<Image>();
                    text = objectRT.GetComponent<TextMeshProUGUI>();

                    TryGetButtonClickEffect()?.OnValidate(this);
                    TryGetTransformEffect()?.OnValidate(this);
                    TryGetScaleEffect()?.OnValidate(this);
                    TryGetColorEffect()?.OnValidate(this);
                    TryGetMaterialEffect()?.OnValidate(this);
                }
#endif
            }

            #region FUNCTIONS

            public bool ActivateOnEnable
            {
                get => activateOnEnable;
                set => activateOnEnable = value;
            }

            #endregion

            #region VIRTUAL

            public virtual void SetActiveEffect(bool isActive, bool fastChangeEffect = false)
            {
                if (this.isActive != isActive || ignoreActivity)
                {
                    this.isActive = isActive;

                    TryGetButtonClickEffect()?.SetActiveEffect(isActive, fastChangeEffect);
                    TryGetTransformEffect()?.SetActiveEffect(isActive, fastChangeEffect);
                    TryGetScaleEffect()?.SetActiveEffect(isActive, fastChangeEffect);
                    TryGetColorEffect()?.SetActiveEffect(isActive, fastChangeEffect);
                    TryGetMaterialEffect()?.SetActiveEffect(isActive, fastChangeEffect);

                    if (ignoreActivity)
                        this.isActive = false;
                }
            }

            public virtual ButtonClickEffect TryGetButtonClickEffect()
            {
                return null;
            }

            public virtual TransformEffect TryGetTransformEffect()
            {
                return null;
            }

            public virtual ScaleEffect TryGetScaleEffect()
            {
                return null;
            }

            public virtual ColorEffect TryGetColorEffect()
            {
                return null;
            }

            public virtual MaterialEffect TryGetMaterialEffect()
            {
                return null;
            }

            protected virtual void Init()
            {
                TryGetButtonClickEffect()?.Init(this);
                TryGetTransformEffect()?.Init(this);
                TryGetScaleEffect()?.Init(this);
                TryGetColorEffect()?.Init(this);
                TryGetMaterialEffect()?.Init(this);
            }

            #endregion

            #region PUBLIC

            public void SetUseEffects(bool useEffects)
            {
                ScaleEffect scaleEffect = TryGetScaleEffect();
                if (scaleEffect != null)
                    scaleEffect.UseEffect = useEffects;

                ColorEffect colorEffect = TryGetColorEffect();
                if (colorEffect != null)
                    colorEffect.UseEffect = useEffects;
            }

            public void SetScaleEndValue(Vector3 startValue, Vector2 endValue)
            {
                ScaleEffect scaleEffect = TryGetScaleEffect();
                if (scaleEffect != null)
                    scaleEffect.SetEndValue(startValue, endValue);
            }

            public void SetColors(StartColor.StartColorIndicator startColor, StartColor.StartColorIndicator endColor)
            {
                ColorEffect colorEffect = TryGetColorEffect();
                if (colorEffect != null)
                    colorEffect.SetColors(startColor, endColor);
            }

            #endregion

            [System.Serializable]
            public class ButtonClickEffect : ModuleEffectBase
            {
                #region Attributes

                [Header("ATTRIBUTES")]
                [SerializeField] private StartColor.StartColorIndicator clickColorIndicator = StartColor.StartColorIndicator.HANDLE_BRIGHT;

                [Header("DEBUG")]
                [SerializeField] private Color endColor;

                private Color startColor;

                #endregion

                #region OVERRIDE

                public override void Init(EffectBaseUIElement effectBase)
                {
                    base.Init(effectBase);

                    if (effectBase.image != null)
                        startColor = effectBase.image.color;
                }

                protected override void OnEnableEffect()
                {
                    DoColorUp();
                }

                protected override void OnDisableEffect(bool fastChangeEffect)
                {
#if UNITY_EDITOR
                    if (!Application.isPlaying)
                    {
                        SetColorFast(startColor);
                        return;
                    }
#endif

                    if (effectBase.image != null)
                    {
                        effectBase.image.DOComplete();
                        if (fastChangeEffect)
                        {
                            effectBase.image.color = startColor;
                            return;
                        }

                        effectBase.image.DOColor(startColor, duration);

                        return;
                    }
                }

                public override void OnValidate(EffectBaseUIElement effectBase)
                {
#if UNITY_EDITOR
                    base.OnValidate(effectBase);

                    endColor = StartColor.GetColor(clickColorIndicator, null);
                    if (effectBase.image)
                        startColor = effectBase.image.color;
#endif
                }

                #endregion

                #region PRIVATE

                private void DoColorUp()
                {
                    if (!isActive)
                        return;

                    effectBase.image.DOColor(endColor, duration).OnComplete(DoColorDown);
                }

                private void DoColorDown()
                {
                    if (!isActive)
                        return;

                    effectBase.image.DOColor(startColor, duration).OnComplete(looping ? DoColorUp : null);
                    if (effectBase.ignoreActivity)
                        isActive = false;
                }

                private void SetColorFast(Color newColor)
                {
                    if (effectBase != null && effectBase.image != null)
                        effectBase.image.color = newColor;
                }

                #endregion
            }

            [System.Serializable]
            public class TransformEffect : ModuleEffectBase
            {
                #region Attributes

                [Header("ROTATION")]
                [SerializeField] private bool useRotation;
                [SerializeField] private bool revertRotation;
                [SerializeField] private float endRotZ;

                #endregion

                #region OVERRIDE

                protected override void OnEnableEffect()
                {
                    effectBase.objectRT.DOComplete();

                    DoRotateUp();
                }

                protected override void OnDisableEffect(bool fastChangeEffect)
                {
#if UNITY_EDITOR
                    if (!Application.isPlaying)
                    {
                        effectBase.objectRT.localEulerAngles = Vector3.zero;
                        return;
                    }
#endif

                    effectBase.objectRT.DOComplete();
                    if (fastChangeEffect)
                    {
                        effectBase.objectRT.DOLocalRotate(Vector3.zero, 0);
                        return;
                    }
                    DoRotateDown();
                }

                #endregion

                #region PRIVATE

                private void DoRotateUp()
                {
                    if (!isActive || !useRotation)
                        return;

                    effectBase.objectRT.DOLocalRotate(new Vector3(0, 0, endRotZ), duration).OnComplete(looping ? DoRotateDown : null);
                }

                private void DoRotateDown()
                {
                    if (!useRotation)
                        return;

                    effectBase.objectRT.DOLocalRotate(new Vector3(0, 0, revertRotation ? -endRotZ : 0f), duration).OnComplete(looping ? DoRotateUp : null);
                }

                #endregion
            }

            [System.Serializable]
            public class ScaleEffect : ModuleEffectBase
            {
                #region Attributes

                public enum ScaleMode
                {
                    LOCAL_SCALE,
                    SIZE_DELTA,
                }

                [Header("ATRTIBUTES")]
                [SerializeField] private ScaleMode scaleMode;

                [Header("SIZE")]
                [SerializeField] private Vector2 endValue;

                private Vector3 startSize;

                #endregion

                #region OVERRIDE

                public override void Init(EffectBaseUIElement effectBase)
                {
                    base.Init(effectBase);

                    SetStartSize();
                }

                protected override void OnEnableEffect()
                {
#if UNITY_EDITOR
                    if (!Application.isPlaying)
                    {
                        SetSizeFast(new Vector3(endValue.x, endValue.x, endValue.x));
                        return;
                    }
#endif

                    DoScaleUp();
                }

                protected override void OnDisableEffect(bool fastChangeEffect)
                {
#if UNITY_EDITOR
                    if (!Application.isPlaying)
                    {
                        SetSizeFast(startSize);
                        return;
                    }
#endif

                    effectBase.objectRT.DOComplete();
                    if (scaleMode == ScaleMode.LOCAL_SCALE)
                        effectBase.objectRT.DOScale(startSize, fastChangeEffect ? 0f : duration);
                    else if (scaleMode == ScaleMode.SIZE_DELTA)
                        effectBase.objectRT.DOSizeDelta(startSize, fastChangeEffect ? 0f : duration);
                }

                public override void OnValidate(EffectBaseUIElement effectBase)
                {
#if UNITY_EDITOR
                    base.OnValidate(effectBase);

                    SetStartSize();
                    SetEndValue(startSize, endValue);
#endif
                }

                #endregion

                #region PUBLIC

                public void SetEndValue(Vector3 startValue, Vector2 endValue)
                {
                    this.endValue = endValue;
                    this.startSize = startValue;
                    if (scaleMode == ScaleMode.LOCAL_SCALE)
                        this.endValue = new Vector2(this.endValue.x, -1);

                    if (effectBase != null)
                        SetSizeFast(startValue);
                }

                #endregion

                #region PRIVATE

                private void DoScaleUp()
                {
                    if (!isActive)
                        return;

                    if (scaleMode == ScaleMode.LOCAL_SCALE)
                        effectBase.objectRT.DOScale(endValue.x, duration).OnComplete(looping ? DoScaleDown : null);
                    else if (scaleMode == ScaleMode.SIZE_DELTA)
                        effectBase.objectRT.DOSizeDelta(endValue, duration).OnComplete(looping ? DoScaleDown : null);
                }

                private void DoScaleDown()
                {
                    if (!isActive)
                        return;

                    if (scaleMode == ScaleMode.LOCAL_SCALE)
                        effectBase.objectRT.DOScale(startSize, duration).OnComplete(looping ? DoScaleUp : null);
                    else if (scaleMode == ScaleMode.SIZE_DELTA)
                        effectBase.objectRT.DOSizeDelta(startSize, duration).OnComplete(looping ? DoScaleUp : null);
                }

                private void SetStartSize()
                {
                    if (scaleMode == ScaleMode.LOCAL_SCALE)
                        startSize = effectBase.objectRT.transform.localScale;
                    else if (scaleMode == ScaleMode.SIZE_DELTA)
                    {
                        startSize = effectBase.objectRT.sizeDelta;
                        startSize.z = 0f;
                    }
                }

                private void SetSizeFast(Vector3 newSize)
                {
                    if (scaleMode == ScaleMode.LOCAL_SCALE)
                        effectBase.objectRT.localScale = newSize;
                    else if (scaleMode == ScaleMode.SIZE_DELTA)
                        effectBase.objectRT.sizeDelta = newSize;
                }

                #endregion
            }

            [System.Serializable]
            public class ColorEffect : ModuleEffectBase
            {
                #region Attributes

                [Header("ATTRIBUTES")]
                [SerializeField] private StartColor.StartColorIndicator endColorIndicator;

                [Header("DEBUG")]
                [SerializeField] private Color endColor;

                private Color startColor;

                #endregion

                #region OVERRIDE

                public override void Init(EffectBaseUIElement effectBase)
                {
                    base.Init(effectBase);

                    if (effectBase.image != null || effectBase.text != null)
                        startColor = effectBase.image != null ? effectBase.image.color : effectBase.text.color;
                }

                protected override void OnEnableEffect()
                {
#if UNITY_EDITOR
                    if (!Application.isPlaying)
                    {
                        SetColorFast(endColor);
                        return;
                    }
#endif

                    DoColorUp();
                }

                protected override void OnDisableEffect(bool fastChangeEffect)
                {
#if UNITY_EDITOR
                    if (!Application.isPlaying)
                    {
                        SetColorFast(startColor);
                        return;
                    }
#endif

                    if (effectBase.image != null)
                    {
                        effectBase.image.DOComplete();
                        if (fastChangeEffect)
                        {
                            effectBase.image.color = startColor;
                            return;
                        }

                        effectBase.image.DOColor(startColor, duration);

                        return;
                    }

                    if (effectBase.text != null)
                    {
                        effectBase.text.DOComplete();
                        if (fastChangeEffect)
                        {
                            effectBase.text.color = startColor;
                            return;
                        }

                        effectBase.text.DOColor(startColor, duration);
                    }
                }

                public override void OnValidate(EffectBaseUIElement effectBase)
                {
#if UNITY_EDITOR
                    base.OnValidate(effectBase);
                    
                    endColor = StartColor.GetColor(endColorIndicator, null);

                    if (effectBase.image != null || effectBase.text != null)
                        startColor = effectBase.image != null ? effectBase.image.color : effectBase.text.color;
#endif
                }

                #endregion

                #region PUBLIC

                public void SetColors(StartColor.StartColorIndicator startColor, StartColor.StartColorIndicator endColor)
                {
                    endColorIndicator = endColor;

                    this.endColor = StartColor.GetColor(endColor, null);
                    this.startColor = StartColor.GetColor(startColor, null);

                    SetColorFast(this.startColor);
                }

                #endregion

                #region PRIVATE

                private void DoColorUp()
                {
                    if (!isActive)
                        return;

                    if (effectBase.image != null)
                        effectBase.image.DOColor(endColor, duration).OnComplete(looping ? DoColorDown : null);
                    else if (effectBase.text != null)
                        effectBase.text.DOColor(endColor, duration).OnComplete(looping ? DoColorDown : null);
                }

                private void DoColorDown()
                {
                    if (!isActive)
                        return;

                    if (effectBase.image != null)
                        effectBase.image.DOColor(startColor, duration).OnComplete(looping ? DoColorUp : null);
                    else if (effectBase.text != null)
                        effectBase.text.DOColor(startColor, duration).OnComplete(looping ? DoColorUp : null);
                }

                private void SetColorFast(Color newColor)
                {
                    if (effectBase == null)
                        return;

                    if (effectBase.image != null)
                        effectBase.image.color = newColor;

                    if (effectBase.text != null)
                        effectBase.text.color = newColor;
                }

                #endregion
            }

            [System.Serializable]
            public class MaterialEffect : ModuleEffectBase
            {
                #region Attributes

                [Header("OFFSET")]
                [SerializeField] private bool useOffset;

                [Header("REFERENCES")]
                [SerializeField] private Material material;

                #endregion

                #region FUNCTIONS

                public Material Material
                {
                    get => material;
                }

                #endregion

                #region OVERRIDE

                public override void Init(EffectBaseUIElement effectBase)
                {
                    base.Init(effectBase);

                    SetOffsetFast(Vector2.zero);
                }

                protected override void OnEnableEffect()
                {
                    DoOffsetUp();
                }

                protected override void OnDisableEffect(bool fastChangeEffect)
                {
                    SetOffsetFast(Vector2.zero);
                }

                public override void OnValidate(EffectBaseUIElement effectBase)
                {
#if UNITY_EDITOR
                    base.OnValidate(effectBase);

                    if (effectBase.image != null)
                        material = effectBase.image.material;
#endif
                }

                #endregion

                #region PRIVATE

                private void DoOffsetUp()
                {
                    if (!isActive)
                        return;

                    if (useOffset)
                        material.DOOffset(new Vector2(material.mainTextureOffset.x + 1f, 0), duration).SetEase(Ease.Linear).OnComplete(DoOffsetDown);
                }

                private void DoOffsetDown()
                {
                    if (!isActive)
                        return;

                    if (useOffset)
                        material.DOOffset(new Vector2(material.mainTextureOffset.x + 1f, 0), duration).SetEase(Ease.Linear).OnComplete(DoOffsetUp);
                }

                private void SetOffsetFast(Vector2 offset)
                {
                    if (effectBase == null)
                        return;

                    if (useOffset)
                        material.mainTextureOffset = offset;
                }

                #endregion
            }

            [System.Serializable]
            public class ModuleEffectBase
            {
                #region Attributes

                [Header("ATTRIBUTES")]
                [SerializeField] protected bool useEffect;
                [SerializeField] protected bool looping;

                [Header("TIME")]
                [SerializeField] protected float duration = 1;

                [Header("REFERENCES")]
                [SerializeField] protected EffectBaseUIElement effectBase;

                public bool isActive { get; protected set; }

                #endregion

                #region FUNCTIONS

                public bool UseEffect
                {
                    get => useEffect;
                    set => useEffect = value;
                }

                public float Duration
                {
                    get => duration;
                }

                #endregion

                #region VIRTUAL

                public virtual void Init(EffectBaseUIElement effectBase)
                {
                    if (this.effectBase == null)
                        this.effectBase = effectBase;
                }

                public virtual bool SetActiveEffect(bool isActive, bool fastChangeEffect = false)
                {
                    if (!effectBase.ignoreActivity && (this.isActive == isActive || !useEffect))
                        return false;

                    this.isActive = isActive;

                    if (isActive)
                    {
                        OnEnableEffect();
                        return true;
                    }

                    OnDisableEffect(fastChangeEffect);

                    return true;
                }

                protected virtual void OnEnableEffect()
                {

                }

                protected virtual void OnDisableEffect(bool fastChangeEffect)
                {

                }

                // DEBUG

                public virtual void OnValidate(EffectBaseUIElement effectBase)
                {
#if UNITY_EDITOR
                    if (this.effectBase == null)
                        this.effectBase = effectBase;
#endif
                }

                #endregion
            }
        }
    }
}