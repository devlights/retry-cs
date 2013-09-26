namespace Devlights.Samples.Retry
{
  using System;
  using System.Collections.Generic;
  using System.Linq;
  using System.Threading;
  
  /// <summary>
  /// リトライ処理を汎用的に行えるユーティリティクラスです。
  /// 指定されたリトライ回数とインターバルで処理をリトライ付きで実行します。
  /// 処理実行時にエラーが発生した場合にエラーコールバックが呼ばれるようにすることもできます。
  /// </summary>
  /// <remarks>
  /// [作成する際に参考にした情報]
  /// http://d.hatena.ne.jp/Yoshiori/20120315/1331825419
  /// https://github.com/yoshiori/retry-handler
  /// http://tnakamura.hatenablog.com/entry/20120327/retry_handler
  /// https://github.com/devlights/retry-java
  /// </remarks>
  public static class RetryHandler
  {
    /// <summary>
    /// 指定された情報を元にリトライ処理付きでactionを実行します。
    /// 処理が試行される回数は、（一度目の実行 + リトライ回数）です。
    /// エラーが発生すると呼び元に<see cref="RetryException"/>がスローされます。
    /// </summary>
    /// <param name="retryCount">リトライ回数</param>
    /// <param name="interval">インターバル (ミリ秒)</param>
    /// <param name="action">実行するアクション</param>
    public static void Execute(int retryCount, int interval, Action action)
    {
      Execute(retryCount, interval, action, null);
    }

    /// <summary>
    /// 指定された情報を元にリトライ処理付きでactionを実行します。
    /// 処理が試行される回数は、（一度目の実行 + リトライ回数）です。
    /// エラーコールバックを指定している場合、エラーが発生すると指定されたエラーコールバックが呼ばれます。
    /// エラーコールバックを指定していない場合で、エラーが発生すると呼び元に<see cref="RetryException"/>がスローされます。
    /// </summary>
    /// <param name="retryCount">リトライ回数</param>
    /// <param name="interval">インターバル</param>
    /// <param name="action">実行するアクション</param>
    /// <param name="errorCallback">エラー発生時に呼ばれるコールバック</param>
    public static void Execute(int retryCount, int interval, Action action, Action<ErrorInfo> errorCallback)
    {
      int activeRetryCount = 0;
      List<Exception> exList = new List<Exception>();

      bool stopThrowException = false;
      try
      {
        for (int i = 0; i < (retryCount + 1); i++)
        {
          try
          {
            action();

            activeRetryCount = (i + 1);
            stopThrowException = true;

            break;
          }
          catch (Exception ex)
          {
            exList.Add(ex);

            bool isInitialProc = true;
            bool retryStop = false;
            try
            {
              if (activeRetryCount != 0)
              {
                isInitialProc = false;

                if (errorCallback != null)
                {
                  ErrorInfo info = new ErrorInfo(activeRetryCount, ex);
                  errorCallback(info);

                  if (info.RetryStop)
                  {
                    retryStop = true;
                    break;
                  }
                }

                if (activeRetryCount < retryCount)
                {
                  Thread.Sleep(interval);
                }
              }
            }
            finally
            {
              if ((activeRetryCount <= retryCount) && isInitialProc || !retryStop)
              {
                activeRetryCount++;
              }
            }
          }
        }
      }
      finally
      {
        if (!stopThrowException)
        {
          if (exList.Count != 0 && errorCallback == null)
          {
            throw new RetryException(exList);
          }
        }
      }
    }
  }

  /// <summary>
  /// エラーコールバックに引き渡されるデータクラスです。
  /// </summary>
  public class ErrorInfo
  {
    /// <summary>
    /// コンストラクタ
    /// </summary>
    /// <param name="currentRetryCount">現在のリトライ回数</param>
    /// <param name="cause">原因</param>
    public ErrorInfo(int currentRetryCount, Exception cause)
    {
      RetryCount = currentRetryCount;
      Cause      = cause;
      RetryStop  = false;
    }

    /// <summary>
    /// リトライ回数
    /// </summary>
    public int RetryCount { get; protected set; }
    /// <summary>
    /// 原因
    /// </summary>
    public Exception Cause { get; protected set; }
    /// <summary>
    /// リトライを中断するか否か.
    /// </summary>
    public bool RetryStop { internal get; set; }
  }

  /// <summary>
  /// リトライオーバーが発生した場合にスローされる例外クラスです。
  /// </summary>
  public class RetryException : Exception
  {
    /// <summary>
    /// コンストラクタ
    /// </summary>
    /// <param name="exList">リトライオーバーするまでに発生した例外リスト</param>
    public RetryException(List<Exception> exList)
    {
      ExceptionList = exList;
    }

    /// <summary>
    /// リトライオーバーするまでに発生した例外リスト
    /// </summary>
    public List<Exception> ExceptionList { get; protected set; }
  }
}
