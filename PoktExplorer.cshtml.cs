using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace WebApp.Pages
{
    public class PoktExplorerModel : PageModel
    {
        public string Query;
        public bool Top200;
        public bool NewestFirst;
        public bool IncludeReceived;
        public bool ShowAll;

        public void OnGet(string Query, string Top200, string NewestFirst, string IncludeReceived, string ShowAll)
        {
            this.Query = string.IsNullOrEmpty(Query) ? null : Query.Trim();
            this.ShowAll = string.IsNullOrEmpty(ShowAll) ? false : ShowAll.ToLower() == "true";
            this.Top200 = string.IsNullOrEmpty(Top200) ? true : Top200.ToLower() == "true";
            this.NewestFirst = string.IsNullOrEmpty(NewestFirst) ? true : NewestFirst.ToLower() == "true";
            this.IncludeReceived = string.IsNullOrEmpty(IncludeReceived) ? true : IncludeReceived.ToLower() == "true";
        }
    }
}
