using UnityEngine;
using System;
using UnityEngine.EventSystems;
using UnityEngine.Events;

public interface ISwipeInput
{
    SwipeEvent OnSwipeStart { get; }
    SwipeEvent OnSwipeMove { get; }
    SwipeEvent OnSwipeEnd { get; }
    SwipeEvent OnSwipeCancel { get; }
}

public class SwipeEvent: UnityEvent<SwipeInfo> {}

public class SwipeInfo 
{
    public Vector2 touchBegin;
    public Vector2 direction;
    public Vector2 directionFirst;
    public Vector2 touchDelta;
    public float timeDelta;
    public bool hasFreePass;
}

public class SwipeInput: MonoBehaviour, ISwipeInput
{
    public SwipeEvent OnSwipeStart { get { return onSwipeStart; } }
    readonly SwipeEvent onSwipeStart = new SwipeEvent();
    public SwipeEvent OnSwipeEnd { get { return onSwipeEnd; } }
    readonly SwipeEvent onSwipeEnd = new SwipeEvent();
    public SwipeEvent OnSwipeMove { get { return onSwipeMove; } }
    readonly SwipeEvent onSwipeMove = new SwipeEvent();
    public SwipeEvent OnSwipeCancel { get { return onSwipeCancel; } }
    readonly SwipeEvent onSwipeCancel = new SwipeEvent();
    
    float timeBegin = 0;
    float timeEnd = 0;
    Vector2 touchBegin = Vector2.zero;
    Vector2 touchEnd = Vector2.zero;
    Vector2 directionFirst = Vector2.zero;
    bool isTouchDown = false;

    void Update () 
    {
        KeyboardUpdate();
        TouchUpdate();
    }

    void TouchUpdate() 
    {
        bool isPointerOverGui = (EventSystem.current) ? EventSystem.current.IsPointerOverGameObject() : false;
#if UNITY_IOS || UNITY_ANDROID
        ReadTouchInput(isPointerOverGui);
#endif
#if UNITY_EDITOR
        ReadMouseInput(isPointerOverGui);
#endif

        JudgeInputIsRight();
    }

    void JudgeInputIsRight()
    {
        if (timeBegin > 0 && timeEnd > 0) 
        {
            float timeDelta = timeEnd - timeBegin;

            Vector2 touchDelta = touchEnd - touchBegin;
            Vector2 direction = GetDirection(ref touchDelta);
            if (direction != Vector2.zero) 
            {
                if (directionFirst == Vector2.zero) {
                    directionFirst = direction;
                }

                var swipeInfo = new SwipeInfo {
                    touchBegin = touchBegin,
                    direction = direction, 
                    directionFirst = directionFirst,
                    touchDelta = touchDelta, 
                    timeDelta = timeDelta,
                    hasFreePass = false,
                };

                if (directionFirst != direction) {
                    onSwipeCancel.Invoke(null);
                    Reset();
                } else if (isTouchDown) {
                    onSwipeMove.Invoke(swipeInfo);
                } else {
                    onSwipeEnd.Invoke(swipeInfo);
                    Reset();
                }
            }
        }
    }

    Vector2 GetDirection(ref Vector2 touchDelta)
    {
        float absoluteX = Math.Abs(touchDelta.x);
        float absoluteY = Math.Abs(touchDelta.y);
        Vector2 direction = Vector2.zero;

        if (absoluteX > absoluteY)
        {
            if (touchDelta.x > 0)
            {
                direction = Vector2.right;
            } 
            else 
            {
                direction = Vector2.left;
            }
        } 
        else if (absoluteX < absoluteY)
        {
            if (touchDelta.y > 0) 
            {
                direction = Vector2.up;
            } 
            else 
            {
                direction = Vector2.down;
            }
        }

        return direction;
    }

    void ReadTouchInput(bool isPointerOverGui)
    {
        if (Input.touchCount > 0 && !isPointerOverGui)
        {
            var firstTouch = Input.GetTouch(0);
            var touchPhase = firstTouch.phase;
            var touchPosition = firstTouch.position;

            if (touchPhase == TouchPhase.Began)
            {
                timeBegin = Time.time;
                touchBegin = touchPosition;
                isTouchDown = true;
                onSwipeStart.Invoke(new SwipeInfo{ touchBegin=touchBegin });
            }

            if (touchBegin != Vector2.zero)
            {
                timeEnd = Time.time;
                touchEnd = touchPosition;
            }

            if (touchPhase == TouchPhase.Ended)
            {
                isTouchDown = false;
            }

            if (touchPhase == TouchPhase.Canceled)
            {
                onSwipeCancel.Invoke(null);
                Reset();
            }
        }
    }

    void ReadMouseInput(bool isPointerOverGui)
    {
        if (Input.GetMouseButtonDown(0) && !isPointerOverGui) 
        {
            timeBegin = Time.time;
            touchBegin = Input.mousePosition;
            isTouchDown = true;
            onSwipeStart.Invoke(new SwipeInfo{ touchBegin=touchBegin });
        }

        if (touchBegin != Vector2.zero) 
        {
            timeEnd = Time.time;
            touchEnd = Input.mousePosition;
        }

        if (Input.GetMouseButtonUp(0))
        {
            isTouchDown = false;
        }
    }

    void Reset()
    {
        directionFirst = touchBegin = touchEnd = Vector2.zero;
        timeBegin = timeEnd = 0;
        isTouchDown = false;
    }

    void KeyboardUpdate() 
    {
        Vector2 direction = Vector2.zero;
        if (Input.GetKeyDown(KeyCode.LeftArrow)) 
        {
            direction = Vector2.left;
        } 
        else if (Input.GetKeyDown(KeyCode.RightArrow)) 
        {
            direction = Vector2.right;
        } 
        else if (Input.GetKeyDown(KeyCode.UpArrow)) 
        {
            direction = Vector2.up;
        } 
        else if (Input.GetKeyDown(KeyCode.DownArrow)) 
        {
            direction = Vector2.down;
        }

        if (direction != Vector2.zero) 
        {
            onSwipeEnd.Invoke(new SwipeInfo(){
                direction = direction, 
                hasFreePass  = true
            });
        }
    }
}