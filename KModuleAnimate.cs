﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

/// <summary>
/// The base 'Super Class' for animation modules in the framework.
/// </summary>
public abstract class KModuleAnimate : PartModule
{
    #region KSPFields
    /// <summary>
    /// The name of the animation to be played with this module.
    /// </summary>
    [KPartModuleFieldConfigurationDocumentation(
        "nameOfAnimation", 
        "The name of the animation to be played with this module.")]
    [KSPFieldDebug("AnimationName", isPersistant=true)]
    public string AnimationName = string.Empty;

    /// <summary>
    /// When True the animation will be played in reverse.
    /// </summary>
    [KPartModuleFieldConfigurationDocumentation(
        "False", 
        "When True the animation will be played in reverse.")]
    [KSPFieldDebug("PlayInReverse", isPersistant = true)]
    public bool PlayInReverse = false;

    /// <summary>
    /// When TRUE, the animation will not be updated.
    /// </summary>
    [KPartModuleFieldConfigurationDocumentation(
        "False", 
        "When TRUE, the animation will not be updated.")]
    [KSPFieldDebug("AnimationIsLocked?", isPersistant = true)]
    public bool IsLocked = false;

    /// <summary>
    /// When TRUE, the Normalized Animation Time of the module is
    /// interpolated between frames based on the 'LerpDampening' 
    /// value.
    /// </summary>
    [KPartModuleFieldConfigurationDocumentation(
        "True",
        "When TRUE, the Normalized Animation Time of the module is" +
        "\n\t//interpolated between frames based on the 'LerpDampening'" +
        "\n\t//value.")]
    [KSPFieldDebug("UseInterpolation?", isPersistant = true)]
    public bool UseInterpolation = true;

    /// <summary>
    /// The coefficient applied to Time.deltaTime that dictates 
    /// the speed our animation is played when interpolating,
    /// as large changes in the normalized time could result in
    /// undesired playback.
    /// </summary>
    [KPartModuleFieldConfigurationDocumentation(
        "1",
        "The coefficient applied to Time.deltaTime that dictates" +
        "\n\t//the speed our animation is played when interpolating, " +
        "\n\t//as large changes in the normalized time could result in" +
        "\n\t//undesired playback.")]
    [KSPFieldDebug("LerpDamp", isPersistant = true)]
    public float LerpDampening = 1;
    #endregion

    #region Fields

    protected AnimationState[] _AnimationStates;

    [KSPFieldDebug("IsSetup?")]
    private bool _IsSetup = false;

    [KSPFieldDebug("SetupWasFailed?")]
    private bool _SetupWasFailed = false;

    /// <summary>
    /// The Normalized Animation Time of the previous frame for interpolation purposes.
    /// </summary>
    [KSPFieldDebug("LastNormalTime", isPersistant = true)]
    protected float LastNormalTime = 0;

    /// <summary>
    /// Cached iteration value so we do not have to make a new one every frame.
    /// </summary>
    private int it = 0;

    #endregion

    #region PartModule

    public override void OnAwake()
    {
        base.OnAwake();

        //If requirements are not met for the module
        //Shut down immediately!
        if (!KRequiresModuleAttribute.RequirementsMet(this))
        {
            Debug.LogWarning(
                string.Format(
                    "Module '{0}' requires another module which is not present! Shutting down.", 
                    this.name));
            DestroyImmediate(this);
        }
    }

    public override void OnStart(StartState state)
    {
        SetupAnimation();

        if(!_SetupWasFailed)
            OnModuleStateChanged(state);
    }

    public override void OnUpdate()
    {
        if(IsLocked || !_IsSetup || _SetupWasFailed)
            return;

        if(UseInterpolation) //Interpolate the time.
        {
            this.LastNormalTime =
                        Mathf.Lerp(
                            LastNormalTime,
                            Mathf.Clamp01(SolveNormalTime()),
                            Time.deltaTime * LerpDampening);
        }
        else //Just roll with what we have.
        {
            this.LastNormalTime = Mathf.Clamp01(SolveNormalTime());
        }

        UpdateModule();
        SetAllNormalTimeTo(LastNormalTime);
    }

    public override string GetInfo()
    {
        //Tell them who is coding :)
        return "Powered by: [Kine-Tech Animation Library]";
    }
    #endregion

    /// <summary>
    /// Sets up the animation allowing for children to also setup.
    /// </summary>
    private void SetupAnimation()
    {
        if (_IsSetup)
            return;
        
        Animation[] anims = base.part.FindModelAnimators(this.AnimationName);
        _AnimationStates = new AnimationState[anims.Length];

        for (int it = 0; it < anims.Length; it++)
        {
            Animation current = anims[it];
            AnimationState animationState = current[this.AnimationName];
            animationState.speed = 0f;
            current.Play(this.AnimationName);
            _AnimationStates[it] = animationState;
        }

        if (!SetupModule())
            this.enabled = false;

        _IsSetup = true;
    }

    /// <summary>
    /// Sets the AnimationStates within this animation to the 
    /// specified normal time while keeping the value clamped 
    /// between 0.0f and 1.0f.
    /// </summary>
    /// <param name="normalTime">The Normalized Animation Time to set our Animation States to.</param>
    protected void SetAllNormalTimeTo(float normalTime)
    {
        if(this.IsLocked)
            return;

        normalTime = PlayInReverse ?
            (Mathf.Clamp01(normalTime) * -1) + 1 :
            (Mathf.Clamp01(normalTime));

        for (it = 0; it < _AnimationStates.Length; it++)
            _AnimationStates[it].normalizedTime = normalTime;
    }

    #region Abstract Interface

    /// <summary>
    /// Called when the state of the partmodule has changed.
    /// </summary>
    /// <param name="state">The new state of the module.</param>
    protected virtual void OnModuleStateChanged(StartState state) { }

    /// <summary>
    /// Allows children to set themselves up initially.
    /// </summary>
    /// <returns>When TRUE, setup was successful; otherwise FALSE.</returns>
    protected abstract bool SetupModule();

    /// <summary>
    /// Allows the child to update itself every frame
    /// </summary>
    /// <param name="normalTime">The normalized time of the animation to modify.</param>
    protected abstract void UpdateModule();

    /// <summary>
    /// Allows the child to determine the normalized animation time.
    /// </summary>
    /// <returns>A value from 0.0f to 1.0f describing the current time of the animation.</returns>
    protected abstract float SolveNormalTime();

    #endregion
}
