


using System.Net;
using TestProject1.Helpers;

public class OpenAiApiClient
{
    [Fact]
    public async Task GetSummaryAsync_ReturnsSummary_WhenValidPromptIsProvided()
    {
        //Arrange 
        var mockHttp = new MockHttpMessageHandler();
        var responseJson = @"{
            ""choices"": [
                {""message"": {""content"": ""Test summary""}}
            ]
        }";
        
        mockHttp.When("https://api.openai.com/v1/chat/completions")
            .Respond("application/json", responseJson);
        
        var client = new HttpClient(mockHttp);
        var service = TestFactory.CreateAiService(client, "test-api-key");
        
        //Act
        var result = await service.GetSummaryAsync("prompt");
        
        //Assert
        Assert.Equal("Test summary", result);
    }

    
    [Fact]
    public async Task GetSummaryAsync_ThrowsException_WhenApiReturnError()
    {
        //Arrange
        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When("*").Respond(HttpStatusCode.InternalServerError);
        
        var client = new HttpClient(mockHttp);
        var service = TestFactory.CreateAiService(client, "test-api-key");
        
        //Act & Assert
        await Assert.ThrowsAsync<Exception>(() =>
            service.GetSummaryAsync("prompt"));
    }

    [Fact]
    public async Task GetSummaryAsync_ReturnsEmptyString_WhenApiResponseIsEmpty()
    {
        //Arrange
        var mockHttp = new MockHttpMessageHandler();
        var responseJson = @"{ ""choices"": [] }";

        mockHttp.When("https://api.openai.com/v1/chat/completions")
            .Respond("application/json", responseJson);
        
        var client = new HttpClient(mockHttp);
        var service = TestFactory.CreateAiService(client, "test-api-key");
        
        //Act
        var result = await service.GetSummaryAsync("prompt");
        
        //Assert
        Assert.Equal(string.Empty, result);
        
    }
}