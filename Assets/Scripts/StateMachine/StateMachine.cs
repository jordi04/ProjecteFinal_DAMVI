﻿using System;
using System.Collections.Generic;


public class StateMachine
{
    StateNode current;
    Dictionary<Type, StateNode> nodes = new();
    HashSet<ITransition> anyTransitions = new();

    public void Update()
    {
        var transition = GetTransition();
        if (transition != null)
            ChangeState(transition.To);

        current.State?.Update();
    }

    public void FixedUpdate()
    {
        current.State?.FixedUpdate();
    }

    public void SetState(Istate state)
    {
        current = nodes[state.GetType()];
        current.State?.OnEnter();
    }

    void ChangeState(Istate state)  
    {
        if (state == current.State) return;

        var previousState = current.State;
        var nextState = nodes[state.GetType()].State;

        previousState?.OnExit();
        nextState?.OnEnter();
        current = nodes[state.GetType()];
    }

    ITransition GetTransition()
    {
        foreach (var transition in anyTransitions)
            if (transition.Condition.Evaluate())
                return transition;

        foreach (var transition in current.Transitions)
            if (transition.Condition.Evaluate())
                return transition;

        return null;
    }

    public void AddTransition(Istate from, Istate to, IPredicate condition)
    {
        GetOrAddNode(from).AddTransition(GetOrAddNode(to).State, condition);
    }

    public void AddAnyTransition(Istate to, IPredicate condition)
    {
        anyTransitions.Add(new Transition(GetOrAddNode(to).State, condition));
    }

    StateNode GetOrAddNode(Istate state)
    {
        var node = nodes.GetValueOrDefault(state.GetType());

        if (node == null)
        {
            node = new StateNode(state);
            nodes.Add(state.GetType(), node);
        }

        return node;
    }

    class StateNode
    {
        public Istate State { get; }
        public HashSet<ITransition> Transitions { get; }

        public StateNode(Istate state)
        {
            State = state;
            Transitions = new HashSet<ITransition>();
        }

        public void AddTransition(Istate to, IPredicate condition)
        {
            Transitions.Add(new Transition(to, condition));
        }
    }
}
