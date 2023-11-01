using Microsoft.AspNetCore.Components;

namespace web.Helper;

public static class NavigationManagerExtensionMethods
{
    public static void NavigateToHome(this NavigationManager navigationManager) =>
        navigationManager.NavigateTo(navigationManager.BaseUri, true);

    public static void NavigateToLogin(this NavigationManager navigationManager) =>
        navigationManager.NavigateTo(navigationManager.BaseUri + "login", true);

    /*
    public static void NavigateToLogout(this NavigationManager navigationManager, IConfigService configService) =>
        navigationManager.NavigateTo(
            configService.KEYCLOAK_LOGOUT_ENDPOINT +
            $"?client_id={configService.KEYCLOAK_CLIENT_ID}" +
            $"&post_logout_redirect_uri={navigationManager.BaseUri}",
            true);
            */

    public static void NavigateToManage(this NavigationManager navigationManager) =>
        navigationManager.NavigateTo(navigationManager.BaseUri + "manage", true);
}