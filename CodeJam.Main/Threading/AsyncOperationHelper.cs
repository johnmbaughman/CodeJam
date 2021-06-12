﻿using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Threading;

using JetBrains.Annotations;

namespace CodeJam.Threading
{
	/// <summary>
	/// Extension and utility methods for <see cref="AsyncOperationManager"/> and <see cref="AsyncOperation"/>
	/// </summary>
	[PublicAPI]
	public static class AsyncOperationHelper
	{
		/// <summary>
		/// Returns an <see cref="AsyncOperation"/> for tracking the duration of a particular asynchronous operation.
		/// </summary>
		/// <returns>
		/// An <see cref="AsyncOperation"/> that you can use to track the duration of an asynchronous method invocation.
		/// </returns>
		[Pure, System.Diagnostics.Contracts.Pure]
		public static AsyncOperation CreateOperation() => AsyncOperationManager.CreateOperation(null);

		/// <summary>
		/// Invokes a <paramref name="runner"/> on the thread or context appropriate for the application model.
		/// </summary>
		/// <param name="asyncOp"></param>
		/// <param name="runner">
		/// A <see cref="Action"/> that wraps the delegate to be called when the operation ends.
		/// </param>
		public static void Post(this AsyncOperation asyncOp, [InstantHandle] Action runner)
		{
			Code.NotNull(asyncOp, nameof(asyncOp));
			Code.NotNull(runner, nameof(runner));

			asyncOp.Post(_ => runner(), null);
		}

		/// <summary>
		/// Ends the lifetime of an asynchronous operation.
		/// </summary>
		/// <param name="asyncOp"></param>
		/// <param name="runner">A <see cref="Action"/> that wraps the delegate to be called when the operation ends.</param>
		public static void PostOperationCompleted(this AsyncOperation asyncOp, [InstantHandle] Action runner)
		{
			Code.NotNull(asyncOp, nameof(asyncOp));
			Code.NotNull(runner, nameof(runner));

			asyncOp.PostOperationCompleted(_ => runner(), null);
		}

		/// <summary>
		/// Invokes a <paramref name="runner"/> on the thread or context appropriate for the application model and waits for
		/// it completion.
		/// </summary>
		/// <param name="asyncOp"></param>
		/// <param name="runner">
		/// A <see cref="Action"/> that wraps the delegate to be called when the operation ends.
		/// </param>
		public static void Send(this AsyncOperation asyncOp, [InstantHandle] Action runner)
		{
			Code.NotNull(asyncOp, nameof(asyncOp));
			Code.NotNull(runner, nameof(runner));

			asyncOp.SynchronizationContext.Send(_ => runner(), null);
		}

		/// <summary>
		/// Invokes a <paramref name="runner"/> on the thread or context appropriate for the application model and returns
		/// result.
		/// </summary>
		/// <typeparam name="T">Type of the value returned by <paramref name="runner"/></typeparam>
		/// <param name="asyncOp"></param>
		/// <param name="runner">
		/// A <see cref="Func{TResult}"/> that wraps the delegate to be called when the operation ends.
		/// </param>
		/// <returns>Result of <paramref name="runner"/> execution.</returns>
		public static T? Send<T>(this AsyncOperation asyncOp, [InstantHandle] Func<T> runner)
		{
			Code.NotNull(asyncOp, nameof(asyncOp));
			Code.NotNull(runner, nameof(runner));

			var result = default(T);
			asyncOp.SynchronizationContext.Send(_ => result = runner(), null);
			return result;
		}

		/// <summary>
		/// Gets thread from pool and run <paramref name="runner"/> inside it.
		/// </summary>
		/// <param name="runner">Action to run inside created thread.</param>
		public static void RunAsync([InstantHandle] Action<AsyncOperation> runner)
		{
			Code.NotNull(runner, nameof(runner));

			var asyncOp = CreateOperation();
			ThreadPool.QueueUserWorkItem(_ => runner(asyncOp));
		}

		/// <summary>
		/// Gets thread from pool and run <paramref name="runner"/> inside it.
		/// </summary>
		/// <param name="runner">Action to run inside created thread.</param>
		/// <param name="completeHandler">
		/// Action called after <paramref name="runner"/> complete execution. Synchronized with method calling thread.
		/// </param>
		public static void RunAsync(
			[InstantHandle] Action<AsyncOperation> runner,
			[InstantHandle] Action completeHandler)
		{
			Code.NotNull(runner, nameof(runner));
			Code.NotNull(completeHandler, nameof(completeHandler));

			var asyncOp = CreateOperation();
			ThreadPool.QueueUserWorkItem(
				_ =>
				{
					try
					{
						runner(asyncOp);
					}
					finally
					{
						asyncOp.PostOperationCompleted(completeHandler);
					}
				});
		}
	}
}