namespace MMR.Models.Enums;

public enum WorkStatus
{
    /// <summary>
    /// 未开始
    /// </summary>
    PreStart,
    
    /// <summary>
    /// 已开始
    /// </summary>
    Start,
    
    /// <summary>
    /// 进行中
    /// </summary>
    Running,
    
    /// <summary>
    /// 已结束
    /// </summary>
    End,
    
    /// <summary>
    /// 验收中
    /// </summary>
    Acceptance,
    
    /// <summary>
    /// 已取消
    /// </summary>
    Cancel,
    
    /// <summary>
    /// 已归档
    /// </summary>
    Archive
} 