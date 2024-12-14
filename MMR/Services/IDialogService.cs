using System.Threading.Tasks;

public interface IDialogService
{
    Task<bool> ShowConfirmAsync(string title, string message, string okText = "确定", string cancelText = "取消");
}