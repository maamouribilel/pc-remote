"""ZIP public ad-hoc signature attributes only, using Apple's copyfile PACK callback.

The flat .NET bundle includes PE assemblies whose code signatures live in xattrs.
Do not archive provenance, Finder data, ACLs or unrelated local xattrs.
API: https://github.com/apple-oss-distributions/copyfile/blob/main/copyfile.3
"""
import ctypes
from pathlib import Path
import tempfile
import zipfile


def write_zip(app, archive):
    api = ctypes.CDLL('/usr/lib/libSystem.B.dylib', use_errno=True)
    api.copyfile_state_alloc.restype = ctypes.c_void_p
    api.copyfile_state_free.argtypes = [ctypes.c_void_p]
    api.copyfile_state_set.argtypes = [ctypes.c_void_p, ctypes.c_uint32, ctypes.c_void_p]
    api.copyfile_state_get.argtypes = [ctypes.c_void_p, ctypes.c_uint32, ctypes.c_void_p]
    api.copyfile.argtypes = [ctypes.c_char_p, ctypes.c_char_p, ctypes.c_void_p, ctypes.c_uint32]
    callback_type = ctypes.CFUNCTYPE(ctypes.c_int, ctypes.c_int, ctypes.c_int, ctypes.c_void_p,
                                    ctypes.c_char_p, ctypes.c_char_p, ctypes.c_void_p)
    accepted = set()
    errors = []

    @callback_type
    def filter_attribute(what, stage, state, source, destination, context):
        if what != 5 or stage != 1:  # COPYFILE_COPY_XATTR / COPYFILE_START
            return 0
        name = ctypes.c_char_p()
        if api.copyfile_state_get(state, 9, ctypes.byref(name)) < 0:  # STATE_XATTRNAME
            errors.append(ctypes.get_errno())
            return 2
        if name.value in (b'com.apple.cs.CodeDirectory', b'com.apple.cs.CodeRequirements',
                          b'com.apple.cs.CodeSignature', b'com.apple.cs.CodeEntitlements',
                          b'com.apple.cs.CodeEntitlementsDER'):
            accepted.add(name.value)
            return 0
        return 1  # COPYFILE_SKIP: the attribute is omitted from packed output.

    metadata = {}
    with tempfile.TemporaryDirectory(prefix='pc-signature-metadata-') as scratch, zipfile.ZipFile(archive, 'w', zipfile.ZIP_DEFLATED) as container:
        packed = Path(scratch) / 'signature'
        for source in sorted(app.rglob('*')):
            if not source.is_file():
                continue
            name = app.name + '/' + str(source.relative_to(app))
            container.write(source, name)
            accepted.clear(); errors.clear()
            state = api.copyfile_state_alloc()
            if not state:
                raise RuntimeError('copyfile state allocation failed')
            try:
                if api.copyfile_state_set(state, 6, ctypes.cast(filter_attribute, ctypes.c_void_p)) < 0:
                    raise RuntimeError('copyfile signature filter setup failed')
                if api.copyfile(str(source).encode(), str(packed).encode(), state, (1 << 22) | (1 << 2)) < 0 or errors:
                    raise RuntimeError('copyfile signature packing failed: ' + str(ctypes.get_errno()))
            finally:
                api.copyfile_state_free(state)
            if accepted:
                data = packed.read_bytes()
                if b'com.apple.provenance' in data:
                    raise RuntimeError('Local provenance was not filtered')
                twin = str(Path(name).with_name('._' + source.name))
                info = zipfile.ZipInfo(twin)
                info.create_system = 3
                info.external_attr = 0o100644 << 16
                info.compress_type = zipfile.ZIP_DEFLATED
                container.writestr(info, data)
                import hashlib
                metadata[twin] = hashlib.sha256(data).hexdigest()
            packed.unlink(missing_ok=True)
    return metadata
