using UnityEngine;

namespace ThronefallMP.Components;

public class TweenScale : MonoBehaviour
{
    private bool _tweening;
    private Vector3 _start;
    private Vector3 _end;
    private float _actualDuration;
    private float _duration;
    private float _bounce;
    private float _timer;

    private void Update()
    {
        if (!_tweening)
        {
            return;
        }
        
        _timer += Time.unscaledDeltaTime;
        if (_timer >= _actualDuration)
        {
            _tweening = false;
            transform.localScale = _end;
            return;
        }
        
        var weight = Mathf.Min(_timer / _duration, 1 + _bounce * 2);
        if (weight > 1 + _bounce)
        {
            weight = 2 + 2 * _bounce - weight;
        }
        
        transform.localScale = Vector3.LerpUnclamped(_start, _end, weight);
    }

    public void Tween(Vector3 target, float duration, float bounce = 0)
    {
        _tweening = true;
        _start = transform.localScale;
        _end = target;
        _bounce = bounce;
        _actualDuration = duration;
        _duration = duration / (1 + 2 * _bounce);
        _timer = 0;
    }

    public void Stop()
    {
        _tweening = false;
        transform.localScale = _end;
    }
}