using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
namespace SimpleColony
{
    public sealed class HelpPanel : MonoBehaviour
    {
        RectTransform panel, content;
        Text heading;
        Font font;
        ScrollRect scroll;
        Button back;
        string current = "root";
        readonly Stack<string> history = new Stack<string>();
        readonly List<GameObject> rows = new List<GameObject>();
        float renderedWidth;
        public void Initialize(RectTransform parent,Font font,DescriptionPopup descriptions)
        {
            this.font=font;
            transform.SetParent(parent,false);
            panel=(RectTransform)transform;
            panel.anchorMin=panel.anchorMax=panel.pivot=new Vector2(.5f,.5f);
            gameObject.AddComponent<Image>().color=new Color(.06f,.095f,.13f);
            back=CreateButton(transform,"‹",Back);
            Place((RectTransform)back.transform,12,12,40,44);
            var home=CreateButton(transform,"Справочник",()=>OpenArticle("root",true));
            Place((RectTransform)home.transform,60,12,180,44);
            var close=CreateButton(transform,"×",()=>gameObject.SetActive(false));
            var closeRect=(RectTransform)close.transform;
            closeRect.anchorMin=closeRect.anchorMax=closeRect.pivot=new Vector2(1,1);
            closeRect.anchoredPosition=new Vector2(-12,-12);closeRect.sizeDelta=new Vector2(40,44);
            heading=CreateText(transform,"",20);
            Place(heading.rectTransform,16,64,300,58);
            var viewport=new GameObject("Article viewport",typeof(RectTransform),typeof(Image),typeof(RectMask2D),typeof(ScrollRect));
            viewport.transform.SetParent(transform,false);
            viewport.GetComponent<Image>().color=Color.clear;
            var rect=(RectTransform)viewport.transform;
            rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=new Vector2(16,16);rect.offsetMax=new Vector2(-16,-128);
            content=new GameObject("Article content",typeof(RectTransform)).GetComponent<RectTransform>();content.SetParent(viewport.transform,false);
            content.anchorMin=new Vector2(0,1);content.anchorMax=Vector2.one;content.pivot=new Vector2(.5f,1);content.offsetMin=content.offsetMax=Vector2.zero;
            scroll=viewport.GetComponent<ScrollRect>();scroll.viewport=rect;scroll.content=content;scroll.horizontal=false;scroll.movementType=ScrollRect.MovementType.Clamped;
            Resize(); Render();
        }
        public void OpenArticle(string id,bool resetHistory=false)
        {
            WikiCatalog.Get(id);
            if(resetHistory)history.Clear();
            else if(current!=id)history.Push(current);
            current=id;Render();
        }
        void Back()
        {
            if(history.Count>0)current=history.Pop();
            else current=WikiCatalog.Get(current).ParentId??"root";
            Render();
        }
        void OnEnable() { if(panel!=null){Resize();Render();} }
        void Update() { Resize(); }
        void Resize()
        {
            var parent=(RectTransform)transform.parent;
            float width=Mathf.Min(640,parent.rect.width-24);
            panel.sizeDelta=new Vector2(width,Mathf.Max(240,parent.rect.height-24));
            panel.anchoredPosition=Vector2.zero;
            heading.rectTransform.sizeDelta=new Vector2(width-32,58);
            if(Mathf.Abs(renderedWidth-width)>1)Render();
        }
        void Render()
        {
            if(content==null)return;
            foreach(var row in rows){row.SetActive(false);Destroy(row);}rows.Clear();
            var article=WikiCatalog.Get(current);
            heading.text=article.ParentId==null?article.Title:WikiCatalog.Get(article.ParentId).Title+" / "+article.Title;
            back.interactable=current!="root"||history.Count>0;
            float width=panel.rect.width-32;
            renderedWidth=panel.rect.width;
            float y=0;
            var body=CreateText(content,article.Body,17);
            Place(body.rectTransform,0,0,width,1);
            float height=body.preferredHeight+8;
            body.rectTransform.sizeDelta=new Vector2(width,height);
            rows.Add(body.gameObject);y+=height+12;
            var links=new HashSet<string>();
            foreach(var child in WikiCatalog.Children(current))
            {
                Link(child.Id,width,ref y);links.Add(child.Id);
            }
            bool labelAdded=false;
            foreach(string related in article.Related)
            {
                if(links.Contains(related))continue;
                if(!labelAdded)
                {
                    var label=CreateText(content,"Связанные темы",15);label.color=new Color(.55f,.7f,.77f);
                    Place(label.rectTransform,0,y+8,width,30);rows.Add(label.gameObject);y+=46;labelAdded=true;
                }
                Link(related,width,ref y);links.Add(related);
            }
            content.sizeDelta=new Vector2(0,y);
            content.anchoredPosition=Vector2.zero;
            scroll.verticalNormalizedPosition=1;
        }
        void Link(string id,float width,ref float y)
        {
            var button=CreateButton(content,WikiCatalog.Get(id).Title+"  ›",()=>OpenArticle(id));
            Place((RectTransform)button.transform,0,y,width,52);
            rows.Add(button.gameObject);y+=60;
        }
        Text CreateText(Transform parent,string value,int size)
        {
            var obj=new GameObject("Text",typeof(RectTransform),typeof(Text));obj.transform.SetParent(parent,false);
            var text=obj.GetComponent<Text>();text.font=font;text.fontSize=size;text.text=value;text.color=new Color(.88f,.94f,.97f);text.raycastTarget=false;text.verticalOverflow=VerticalWrapMode.Overflow;
            return text;
        }
        Button CreateButton(Transform parent,string title,UnityEngine.Events.UnityAction action)
        {
            var obj=new GameObject(title,typeof(RectTransform),typeof(Image),typeof(Button));obj.transform.SetParent(parent,false);
            var image=obj.GetComponent<Image>();image.color=new Color(.12f,.19f,.24f);
            var button=obj.GetComponent<Button>();button.targetGraphic=image;button.onClick.AddListener(action);
            var label=CreateText(obj.transform,title,17);label.alignment=TextAnchor.MiddleCenter;
            label.rectTransform.anchorMin=Vector2.zero;label.rectTransform.anchorMax=Vector2.one;label.rectTransform.offsetMin=new Vector2(8,0);label.rectTransform.offsetMax=new Vector2(-8,0);
            return button;
        }
        static void Place(RectTransform rect,float x,float y,float width,float height)
        {
            rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(0,1);rect.anchoredPosition=new Vector2(x,-y);rect.sizeDelta=new Vector2(width,height);
        }
    }
}
