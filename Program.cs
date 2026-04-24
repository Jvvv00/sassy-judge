using System.Text;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options => {
    options.AddPolicy("AllowAll", p => p.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});
builder.Services.AddHttpClient();

var app = builder.Build();

app.UseDefaultFiles(); 
app.UseStaticFiles();  
app.UseCors("AllowAll");

var apiKey = builder.Configuration["Gemini:ApiKey"];
var url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-3-flash-preview:generateContent?key={apiKey}";

app.MapPost("/critique", async (CritiqueRequest req, HttpClient http) =>
{
    try 
    {
        string personality = req.Judge switch {
            "Edward Lee" => "You are Chef Edward Lee. Focus on the story and visual soul of the plate.",
            "Gordon Ramsay" => "You are Gordon Ramsay. Be brutal about the plating and raw ingredients.",
            _ => "You are Chef Ahn Sung-jae. Look for technical perfection and 'ik-him'."
        };

        var promptText = $"{personality} Here is a dish called '{req.Food}'. " +
                         "Critique its appearance and preparation. " +
                         "IMPORTANT: Your response must be exactly two paragraphs long.";

        var payload = new {
            contents = new[] {
                new {
                    parts = new object[] {
                        new { text = promptText },
                        new { inline_data = new { mime_type = "image/jpeg", data = req.ImageContent } }
                    }
                }
            }
        };

        var json = JsonSerializer.Serialize(payload);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await http.PostAsync(url, content);
        var result = await response.Content.ReadAsStringAsync();

        using var doc = JsonDocument.Parse(result);
        var text = doc.RootElement.GetProperty("candidates")[0]
                                  .GetProperty("content")
                                  .GetProperty("parts")[0]
                                  .GetProperty("text")
                                  .GetString();

        return Results.Ok(new { critique = text });
    }
    catch (Exception ex)
    {
        return Results.Problem("The judge is confused: " + ex.Message);
    }
});

app.Run();

public record CritiqueRequest(string Food, string Judge, string ImageContent);