using SharedModelsLib.ApiModels;

namespace StartParametersTest;

public class StartParametersTest
{
    [Fact]
    public void ReturnInitialLoginToken()
    {
        var loginToken = "asdf123";
        var shouldBe = $"+sv_setsteamaccount {loginToken}";
        var startParameters = new StartParameters()
        {
            LoginToken = loginToken
        };
        var loginTokenStartParameter = startParameters.GetLoginToken(null);
        Assert.Equal(shouldBe, loginTokenStartParameter);
    }
    
    [Fact]
    public void ReturnBackupLoginToken()
    {
        var loginToken = "asdf123456";
        var shouldBe = $"+sv_setsteamaccount {loginToken}";
        var startParameters = new StartParameters();
        var loginTokenStartParameter = startParameters.GetLoginToken(loginToken);
        Assert.Equal(shouldBe, loginTokenStartParameter);
    }
    
    [Fact]
    public void ReturnEmptyString()
    {
        var startParameters = new StartParameters
        {
            LoginToken = " "
        };
        var loginTokenStartParameter = startParameters.GetLoginToken(null);
        Assert.True(string.IsNullOrWhiteSpace(loginTokenStartParameter));
    }
}