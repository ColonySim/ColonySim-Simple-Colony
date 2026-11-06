using System;
using System.Collections.Generic;
namespace SimpleColony
{
    public sealed class WikiArticle
    {
        public string Id { get; }
        public string ParentId { get; }
        public string Title { get; }
        public string Body { get; }
        public IReadOnlyList<string> Related { get; }
        internal WikiArticle(string id,string parent,string title,string body,params string[] related)
        {
            Id=id; ParentId=parent; Title=title; Body=body;
            Related=Array.AsReadOnly(related);
        }
    }
}
