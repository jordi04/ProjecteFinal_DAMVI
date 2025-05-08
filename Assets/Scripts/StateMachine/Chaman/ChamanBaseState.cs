using UnityEngine;

public abstract class ChamanBaseState : Istate
{
    protected readonly Chaman chaman;
    protected readonly Animator animator;

    protected static readonly int RunHash = Animator.StringToHash("Run");
    protected static readonly int IdelHash = Animator.StringToHash("Idle");
    protected static readonly int RiseHandsHash = Animator.StringToHash("RiseHands");

    protected const float crossFadeDuration = 0.1f;
    protected ChamanBaseState(Chaman chaman, Animator animator)
    {
        this.chaman = chaman;
        this.animator = animator;
    }
    public virtual void OnEnter()
    {
    }
    
    public virtual void Update()
    {
    }   

    public virtual void FixedUpdate()
    {
    }
    
    public virtual void OnExit()
    {
    }
}
