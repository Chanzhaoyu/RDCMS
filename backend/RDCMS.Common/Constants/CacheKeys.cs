namespace RDCMS.Common.Constants;

public static class CacheKeys
{
    // 强制下线黑名单：管理员强制某用户下线后，该用户的 access token 在剩余有效期内被拒绝
    private const string ForceOfflinePrefix = "rdcms:force-offline:";

    // 用户权限缓存
    private const string PermPrefix = "rdcms:perm:";

    // 用户信息缓存
    private const string UserPrefix = "rdcms:user:";

    public static string ForceOffline(int userId) => $"{ForceOfflinePrefix}{userId}";
    public static string UserPermissions(int userId) => $"{PermPrefix}{userId}";
    public static string UserInfo(int userId) => $"{UserPrefix}{userId}";
}