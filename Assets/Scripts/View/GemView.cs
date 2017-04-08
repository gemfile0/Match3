using System;
using DG.Tweening;
using UnityEngine;

public interface IGemView
{
    int Endurance { get; }
    Position Position { get; }
    GemModel id { get; }
    GemModel UpdateModel(GemModel gemModel);
}

public class GemView: BaseView<GemModel, GemController<GemModel>>
{
    public GemType Type 
    { 
        get { return Model.Type; } 
    }

    private SpriteRenderer spriteRenderer;

    void OnEnable() 
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        movingSequence = DOTween.Sequence();
        movingSequence.SetEase(Ease.InOutQuad);
    }

    internal Position Position 
    { 
        get { return Model.Position; } 
    }

    internal int Endurance 
    {
        get { return Model.endurance; }
    } 

    internal Int64 ID 
    { 
        get { return Model.id; }
    }

    bool showID = false;
    Sequence movingSequence;

    internal void UpdateModel(GemModel gemModel) 
    {
        Model = gemModel;
        Model.SubscribeFalling(isFalling => {
            if (!isFalling) {
                Squash();
            }
        });

        if (showID) 
        {
            var ID = transform.Find("ID");
            if (ID == null) {
                ID = ResourceCache.Instantiate("ID", transform).transform;
            }
            ID.GetComponent<TextMesh>().text = gemModel.id.ToString();
        }

    }
    
    internal void Highlight() 
    {
        var duration = 0.395f;

        MaterialPropertyBlock mpb = new MaterialPropertyBlock();
        spriteRenderer.GetPropertyBlock(mpb);

        DOTween.To(
            () => mpb.GetFloat("_FlashAmount"),
            value => {
                mpb.SetFloat("_FlashAmount", value);
                spriteRenderer.SetPropertyBlock(mpb);
            }, 
            .2f, 
            duration
        ).SetLoops(2, LoopType.Yoyo).SetEase(Ease.InOutSine);
    }

    internal void Squash() 
    {
        var sequence = DOTween.Sequence();
        sequence.Append(transform.DOScale(new Vector3(1.08f, 0.92f, 1), 0.12f));
        sequence.Append(transform.DOScale(new Vector3(1, 1, 1), 0.68f).SetEase(Ease.OutElastic));

        MaterialPropertyBlock mpb = new MaterialPropertyBlock();
        spriteRenderer.GetPropertyBlock(mpb);
        mpb.SetFloat("_FlashAmount", 0.0f);
        spriteRenderer.SetPropertyBlock(mpb);
    }

    internal void Open() 
    {
        gameObject.SetActive(true);

        MaterialPropertyBlock mpb = new MaterialPropertyBlock();
        spriteRenderer.GetPropertyBlock(mpb);
        mpb.SetFloat("_FlashAmount", 0.4f);
        spriteRenderer.SetPropertyBlock(mpb);
    }

    internal void SetBlock() 
    {
        MaterialPropertyBlock mpb = new MaterialPropertyBlock();
        spriteRenderer.GetPropertyBlock(mpb);
        mpb.SetFloat("_FlashAmount", 0.4f);
        mpb.SetColor("_FlashColor", new Color32(255, 0, 0, 1));
        spriteRenderer.SetPropertyBlock(mpb);
    }

    internal void DoLocalMove(Vector3 nextPosition, float duration)
    {
        movingSequence.Append(gameObject.transform.DOLocalMove(
			nextPosition, 
			duration
		).SetEase(Ease.Linear));
    }

    internal void SetActive(bool visible)
    {
        gameObject.SetActive(visible);
    }

    internal void SetLocalPosition(Vector2 position)
    {
        transform.localPosition = position;
    }
}
