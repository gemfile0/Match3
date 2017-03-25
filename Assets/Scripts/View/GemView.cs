using DG.Tweening;
using UnityEngine;

public interface IGemView
{
    Position Position { get; }
    GemModel UpdateModel(GemModel gemModel);
}

public class GemView: BaseView<GemModel, GemController<GemModel>>
{
    private SpriteRenderer spriteRenderer;

    void OnEnable() {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    internal Position Position { 
        get { return Model.position; } 
    }

    internal void UpdateModel(GemModel gemModel) {
        Model = gemModel;
    }
    
    internal void Highlight() {
        var duration = 0.395f;

        MaterialPropertyBlock mpb = new MaterialPropertyBlock();
        spriteRenderer.GetPropertyBlock(mpb);

        DOTween.To(
            () => mpb.GetFloat("_FlashAmount"),
            value => {
                mpb.SetFloat("_FlashAmount", value);
                spriteRenderer.SetPropertyBlock(mpb);
            }, .2f, duration
        ).SetLoops(2, LoopType.Yoyo).SetEase(Ease.InOutSine);
    }

    internal void Squash() {
        var sequence = DOTween.Sequence();
        sequence.Append(transform.DOScale(new Vector3(1.1f, 0.9f, 1), 0.09f));
        sequence.Append(transform.DOScale(new Vector3(1, 1, 1), 0.7f).SetEase(Ease.OutElastic));
    }
}
