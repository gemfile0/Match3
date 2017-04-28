using System;
using DG.Tweening;
using UnityEngine;

public interface IGemView
{
    int Endurance { get; }
    Position Position { get; }
    GemModel id { get; }
    Int64 Deadline { get; }
    Int64 PreservedUntil { get; }
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
        movingSequence.SetEase(Ease.Linear);
    }

    public Position Position 
    { 
        get { return Model.Position; } 
    }

    public int Endurance 
    {
        get { return Model.endurance; }
    } 

    public Int64 ID 
    { 
        get { return Model.id; }
    }

    public Int64 PreservedFromBreak
    {
        get { return Model.preservedFromBreak; }
    }

    public Int64 PreservedFromMatch
    {
        get { return Model.preservedFromMatch; }
    }

    bool showID = true;
    Sequence movingSequence;
    public Position reservedPosition;

    public void UpdateModel(GemModel gemModel) 
    {
        Model = gemModel;
        if (showID) 
        {
            var ID = transform.Find("ID");
            if (ID == null) {
                ID = ResourceCache.Instantiate("ID", transform).transform;
            }
            ID.GetComponent<TextMesh>().text = gemModel.id.ToString();
        }
    }
    
    public void Highlight() 
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

    public void Squash() 
    {
        var sequence = DOTween.Sequence();
        sequence.Append(transform.DOScale(new Vector3(1.08f, 0.92f, 1), 0.12f));
        sequence.Append(transform.DOScale(new Vector3(1, 1, 1), 0.68f).SetEase(Ease.OutElastic));

        MaterialPropertyBlock mpb = new MaterialPropertyBlock();
        spriteRenderer.GetPropertyBlock(mpb);
        mpb.SetFloat("_FlashAmount", 0.0f);
        spriteRenderer.SetPropertyBlock(mpb);
    }

    public void Reveal() 
    {
        gameObject.SetActive(true);

        MaterialPropertyBlock mpb = new MaterialPropertyBlock();
        spriteRenderer.GetPropertyBlock(mpb);
        mpb.SetFloat("_FlashAmount", 0.4f);
        spriteRenderer.SetPropertyBlock(mpb);

        var color = GetComponent<SpriteRenderer>().color;
        GetComponent<SpriteRenderer>().color = new Color(color.r, color.g, color.b, 1f);
    }

    public void Hide()
    {
        var color = GetComponent<SpriteRenderer>().color;
        GetComponent<SpriteRenderer>().color = new Color(color.r, color.g, color.b, 0.5f);
    }

    public void SetBlock() 
    {
        MaterialPropertyBlock mpb = new MaterialPropertyBlock();
        spriteRenderer.GetPropertyBlock(mpb);
        mpb.SetFloat("_FlashAmount", 0.4f);
        mpb.SetColor("_FlashColor", new Color32(255, 0, 0, 1));
        spriteRenderer.SetPropertyBlock(mpb);
    }

    public void DoLocalMove(Vector3 nextPosition, float duration)
    {
        movingSequence.Append(gameObject.transform.DOLocalMove(
			nextPosition, 
			duration
		).SetEase(Ease.Linear));
    }

    public void SetActive(bool visible)
    {
        gameObject.SetActive(visible);
    }

    public void SetLocalPosition(Vector2 position)
    {
        transform.localPosition = position;
    }
}
