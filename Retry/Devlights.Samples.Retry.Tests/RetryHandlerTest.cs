namespace Devlights.Samples.Retry
{
  using System;
  using System.Collections.Generic;
  using Microsoft.VisualStudio.TestTools.UnitTesting;
  
  [TestClass]
  public class RetryHandlerTest
  {
    [TestMethod]
    public void エラーが無い状態だと一回だけ実行される()
    {
      // Arrange
      int retryCount = 3;
      int interval = 500;

      // Act
      List<bool> tmpList = new List<bool>();
      RetryHandler.Execute(retryCount, interval, () =>
      {
        tmpList.Add(true);
      });

      // Assert
      Assert.AreEqual<int>(1, tmpList.Count);
    }

    [TestMethod]
    public void エラーが発生した場合指定された回数分リトライ処理を行う()
    {
      // Arrange
      int retryCount = 3;
      int interval = 100;

      // Act
      List<bool> tmpList = new List<bool>();

      try
      {
        RetryHandler.Execute(retryCount, interval, () =>
        {
          tmpList.Add(true);
          throw new Exception();
        });
      }
      catch (RetryException ex)
      {
        // Assert
        Assert.AreEqual<int>(4, ex.ExceptionList.Count);
      }

      // Assert
      Assert.AreEqual<int>(4, tmpList.Count);
    }

    [TestMethod]
    public void エラーコールバックを指定するとエラー時に呼ばれる()
    {
      // Arrange
      int retryCount = 3;
      int interval = 100;

      // Act
      List<bool> tmpList = new List<bool>();

      int _count = 0;
      RetryHandler.Execute(retryCount, interval, () =>
      {
        tmpList.Add(true);
        throw new Exception();
      }, (info) =>
      {
        // Assert
        Assert.AreEqual<int>(info.RetryCount, ++_count);
        Assert.IsNotNull(info.Cause);
      });

      // Assert
      Assert.AreEqual<int>(4, tmpList.Count);
    }

    [TestMethod]
    public void エラーコールバック内でリトライ停止設定するとリトライが中断される()
    {
      // Arrange
      int retryCount = 3;
      int interval = 100;
    
      // Act
      List<bool> tmpList = new List<bool>();

      int _count = 0;
      RetryHandler.Execute(retryCount, interval, () => 
      {
        tmpList.Add(true);
        throw new Exception();
      }, (info) => 
      {
        // Assert
        Assert.AreEqual<int>(info.RetryCount, ++_count);
        Assert.IsNotNull(info.Cause);
        
        if (_count == 2) 
        {
          info.RetryStop = true;
          return;
        }
      });
    
      // Assert
      Assert.AreEqual<int>(3, tmpList.Count);
    }
  }
}
