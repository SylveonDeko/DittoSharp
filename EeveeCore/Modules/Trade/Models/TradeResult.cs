namespace EeveeCore.Modules.Trade.Models;

/// <summary>
///     Represents the result of a trade operation.
///     Contains success status, error messages, and optional data.
/// </summary>
public class TradeResult
{
    /// <summary>
    ///     Gets or sets a value indicating whether the operation was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    ///     Gets or sets the message describing the result.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets additional data from the operation.
    /// </summary>
    public object? Data { get; set; }

    /// <summary>
    ///     Gets or sets the error code if the operation failed.
    /// </summary>
    public string? ErrorCode { get; set; }

    /// <summary>
    ///     Creates a successful result.
    /// </summary>
    /// <param name="message">The success message.</param>
    /// <param name="data">Optional data to include.</param>
    /// <returns>A successful TradeResult.</returns>
    public static TradeResult FromSuccess(string message, object? data = null)
    {
        return new TradeResult 
        { 
            Success = true, 
            Message = message, 
            Data = data 
        };
    }

    /// <summary>
    ///     Creates a failed result.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="errorCode">Optional error code.</param>
    /// <returns>A failed TradeResult.</returns>
    public static TradeResult Failure(string message, string? errorCode = null)
    {
        return new TradeResult 
        { 
            Success = false, 
            Message = message, 
            ErrorCode = errorCode 
        };
    }

    /// <summary>
    ///     Creates a failed result with a specific error code and data.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="errorCode">The error code.</param>
    /// <param name="data">Optional additional data.</param>
    /// <returns>A failed TradeResult with error code.</returns>
    public static TradeResult FailureWithData(string message, string errorCode, object? data)
    {
        return new TradeResult 
        { 
            Success = false, 
            Message = message, 
            ErrorCode = errorCode,
            Data = data 
        };
    }
}

/// <summary>
///     Represents the result of a gift operation.
///     Extends TradeResult with gift-specific functionality.
/// </summary>
public class GiftResult : TradeResult
{
    /// <summary>
    ///     Gets or sets the user ID who gave the gift.
    /// </summary>
    public ulong? GiverId { get; set; }

    /// <summary>
    ///     Gets or sets the user ID who received the gift.
    /// </summary>
    public ulong? ReceiverId { get; set; }

    /// <summary>
    ///     Gets or sets the type of gift that was given.
    /// </summary>
    public string? GiftType { get; set; }

    /// <summary>
    ///     Gets or sets the amount or quantity of the gift.
    /// </summary>
    public string? Amount { get; set; }

    /// <summary>
    ///     Creates a successful gift result.
    /// </summary>
    /// <param name="message">The success message.</param>
    /// <param name="giverId">The ID of the user who gave the gift.</param>
    /// <param name="receiverId">The ID of the user who received the gift.</param>
    /// <param name="giftType">The type of gift.</param>
    /// <param name="amount">The amount of the gift.</param>
    /// <param name="data">Optional data to include.</param>
    /// <returns>A successful GiftResult.</returns>
    public static GiftResult SuccessfulGift(string message, ulong giverId, ulong receiverId, 
        string giftType, string amount, object? data = null)
    {
        return new GiftResult
        {
            Success = true,
            Message = message,
            GiverId = giverId,
            ReceiverId = receiverId,
            GiftType = giftType,
            Amount = amount,
            Data = data
        };
    }

    /// <summary>
    ///     Creates a failed gift result.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="errorCode">Optional error code.</param>
    /// <returns>A failed GiftResult.</returns>
    public static GiftResult FailedGift(string message, string? errorCode = null)
    {
        return new GiftResult
        {
            Success = false,
            Message = message,
            ErrorCode = errorCode
        };
    }
}