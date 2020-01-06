using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;

public class MyController :
    Controller
{
    public ActionResult<List<DataItem>> Method(string input)
    {
        var headers = HttpContext.Response.Headers;
        headers.Add("headerKey", "headerValue");
        headers.Add("receivedInput", input);

        var cookies = HttpContext.Response.Cookies;
        cookies.Append("cookieKey", "cookieValue");

        var items = new List<DataItem>
        {
            new DataItem("Value1"),
            new DataItem("Value2")
        };
        return new ActionResult<List<DataItem>>(items);
    }

    public class DataItem
    {
        public string Value { get; }

        public DataItem(string value)
        {
            Value = value;
        }
    }
}