using System;

namespace Upsmile.Sync.BasicClasses
{
    /// <summary>
    /// Реализация монады maybe
    /// </summary>
    public static class MaybeMonadic
    {
        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="TInput"></typeparam>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="o"></param>
        /// <param name="evaluator"></param>
        /// <returns></returns>
        public static TResult With<TInput, TResult>(this TInput o,
        Func<TInput, TResult> evaluator)
            where TResult : class
            where TInput : class
        {
            return o == null ? null : evaluator(o);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="TInput"></typeparam>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="o"></param>
        /// <param name="evaluator"></param>
        /// <param name="failureValue"></param>
        /// <returns></returns>
        public static TResult Return<TInput, TResult>(this TInput o,
       Func<TInput, TResult> evaluator, TResult failureValue) where TInput : class
        {
            return o == null ? failureValue : evaluator(o);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="TInput"></typeparam>
        /// <param name="o"></param>
        /// <param name="evaluator"></param>
        /// <returns></returns>
        public static TInput If<TInput>(this TInput o, Func<TInput, bool> evaluator)
       where TInput : class
        {
            if (o == null) return null;
            return evaluator(o) ? o : null;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="TInput"></typeparam>
        /// <param name="o"></param>
        /// <param name="evaluator"></param>
        /// <returns></returns>
        public static TInput Unless<TInput>(this TInput o, Func<TInput, bool> evaluator)
               where TInput : class
        {
            if (o == null) return null;
            return evaluator(o) ? null : o;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="TInput"></typeparam>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="o"></param>
        /// <param name="evaluator"></param>
        /// <returns></returns>
        public static TResult WithValue<TInput, TResult>(this TInput o,
       Func<TInput, TResult> evaluator)
       where TInput : struct
        {
            return evaluator(o);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="TInput"></typeparam>
        /// <param name="o"></param>
        /// <param name="action"></param>
        /// <returns></returns>
        public static TInput Do<TInput>(this TInput o, Action<TInput> action)
        where TInput : class
        {
            if (o == null) return null;
            action(o);
            return o;
        }
    }
}
