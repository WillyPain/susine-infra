namespace Identity.Server.Pages.Shared
{
    using Microsoft.AspNetCore.Razor.TagHelpers;

    [HtmlTargetElement("panel")]
    public class PanelTagHelper : TagHelper
    {
        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            output.TagName = "div"; // Change <panel> to <div>
            output.Attributes.SetAttribute("class", "panel");
            //output.PreContent.SetHtmlContent("<div class='panel-content'>");
            //output.PostContent.SetHtmlContent("</div>");
        }
    }
}
