using System;
using System.Runtime.InteropServices;
using size_t = System.UIntPtr;

internal static partial class Interop
{
#if !DEBUG
    [System.Security.SuppressUnmanagedCodeSecurity]
#endif
    public static unsafe class LibDeflate
    {
        private const CallingConvention CallConv = CallingConvention.StdCall;

        /// <summary>
        /// Result of a call to libdeflate_deflate_decompress(),
        /// libdeflate_zlib_decompress(), or libdeflate_gzip_decompress().
        /// </summary>
        public enum libdeflate_result : int
        {
            /// <summary>
            /// Decompression was successful.
            /// </summary>
            LIBDEFLATE_SUCCESS = 0,

            /// <summary>
            /// Decompressed failed because the compressed data was invalid, corrupt,
            /// or otherwise unsupported.
            /// </summary>
            LIBDEFLATE_BAD_DATA = 1,

            /// <summary>
            /// A NULL 'actual_out_nbytes_ret' was provided, but the data would have
            /// decompressed to fewer than 'out_nbytes_avail' bytes.
            /// </summary>
            LIBDEFLATE_SHORT_OUTPUT = 2,

            /// <summary>
            /// The data would have decompressed to more than 'out_nbytes_avail'
            /// bytes.
            /// </summary>
            LIBDEFLATE_INSUFFICIENT_SPACE = 3,
        }

        /// <summary>
        /// libdeflate_alloc_decompressor() allocates a new decompressor that can be used
        /// for DEFLATE, zlib, and gzip decompression.  The return value is a pointer to
        /// the new decompressor, or NULL if out of memory.
        ///
        /// This function takes no parameters, and the returned decompressor is valid for
        /// decompressing data that was compressed at any compression level and with any
        /// sliding window size.
        ///
        /// A single decompressor is not safe to use by multiple threads concurrently.
        /// However, different threads may use different decompressors concurrently.
        /// </summary>
        [DllImport(Libraries.LibDeflate, CallingConvention = CallConv, EntryPoint = "libdeflate_alloc_decompressor")]
        public static extern IntPtr libdeflate_alloc_decompressor();

        /// <summary>
        /// Like libdeflate_deflate_decompress(), but assumes the zlib wrapper format
        /// instead of raw DEFLATE.
        ///
        /// Decompression will stop at the end of the zlib stream, even if it is shorter
        /// than 'in_nbytes'.  If you need to know exactly where the zlib stream ended,
        /// use libdeflate_zlib_decompress_ex().
        /// </summary>
        [DllImport(Libraries.LibDeflate, CallingConvention = CallConv, EntryPoint = "libdeflate_zlib_decompress")]
        public static extern libdeflate_result libdeflate_zlib_decompress(
            IntPtr decompressor,
            byte* input, size_t inputSize,
            byte* output, size_t outputSize,
            out size_t actual_out_nbytes_ret);

        /// <summary>
        /// Like libdeflate_deflate_decompress(), but adds the 'actual_in_nbytes_ret'
        /// argument. If decompression succeeds and 'actual_in_nbytes_ret' is not NULL,
        /// then the actual compressed size of the DEFLATE stream (aligned to the next
        /// byte boundary) is written to *actual_in_nbytes_ret.
        /// </summary>
        [DllImport(Libraries.LibDeflate, CallingConvention = CallConv, EntryPoint = "libdeflate_zlib_decompress_ex")]
        public static extern libdeflate_result libdeflate_zlib_decompress_ex(
            IntPtr decompressor,
            byte* input, size_t inputSize,
            byte* output, size_t outputSize,
            out size_t actual_in_nbytes_ret,
            out size_t actual_out_nbytes_ret);

        /// <summary>
        /// libdeflate_free_decompressor() frees a decompressor that was allocated with
        /// libdeflate_alloc_decompressor().  If a NULL pointer is passed in, no action
        /// is taken.
        /// </summary>
        [DllImport(Libraries.LibDeflate, CallingConvention = CallConv, EntryPoint = "libdeflate_free_decompressor")]
        public static extern void libdeflate_free_decompressor(IntPtr decompressor);

        /// <summary>
        /// libdeflate_alloc_compressor() allocates a new compressor that supports
        /// DEFLATE, zlib, and gzip compression.  'compression_level' is the compression
        /// level on a zlib-like scale but with a higher maximum value (1 = fastest, 6 =
        /// medium/default, 9 = slow, 12 = slowest).  The return value is a pointer to
        /// the new compressor, or NULL if out of memory.
        ///
        /// Note: for compression, the sliding window size is defined at compilation time
        /// to 32768, the largest size permissible in the DEFLATE format.  It cannot be
        /// changed at runtime.
        ///
        /// A single compressor is not safe to use by multiple threads concurrently.
        /// However, different threads may use different compressors concurrently.
        /// </summary>
        [DllImport(Libraries.LibDeflate, CallingConvention = CallConv, EntryPoint = "libdeflate_alloc_compressor")]
        public static extern IntPtr libdeflate_alloc_compressor(int compression_level);

        /// <summary>
        /// Like libdeflate_deflate_compress(), but stores the data in the zlib wrapper
        /// format.
        /// </summary>
        [DllImport(Libraries.LibDeflate, CallingConvention = CallConv, EntryPoint = "libdeflate_zlib_compress")]
        public static unsafe extern size_t libdeflate_zlib_compress(IntPtr compressor,
             byte* input, size_t inputSize,
             byte* output, size_t outputSize);

        /// <summary>
        /// Like libdeflate_deflate_compress_bound(), but assumes the data will be
        /// compressed with libdeflate_zlib_compress() rather than with
        /// libdeflate_deflate_compress().
        /// </summary>
        [DllImport(Libraries.LibDeflate, CallingConvention = CallConv, EntryPoint = "libdeflate_zlib_compress_bound")]
        public static extern size_t libdeflate_zlib_compress_bound(IntPtr compressor, size_t sourceSize);

        /// <summary>
        /// libdeflate_free_compressor() frees a compressor that was allocated with
        /// libdeflate_alloc_compressor().  If a NULL pointer is passed in, no action is
        /// taken.
        /// </summary>
        [DllImport(Libraries.LibDeflate, CallingConvention = CallConv, EntryPoint = "libdeflate_free_compressor")]
        public static extern void libdeflate_free_compressor(IntPtr compressor);
    }
}
