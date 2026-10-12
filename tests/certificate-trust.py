"""Strict TLS checks against the isolated loopback fixture, using public CA data only."""
from pathlib import Path
import json
import socket
import ssl
import sys
import urllib.request

certificate = Path(sys.argv[1]).read_bytes()
context = ssl.create_default_context(cadata=ssl.DER_cert_to_PEM_cert(certificate))
context.verify_flags |= ssl.VERIFY_X509_STRICT
with urllib.request.urlopen("https://localhost:8443/health", context=context, timeout=10) as reply:
    if reply.status != 200 or not json.load(reply)["ready"]:
        raise RuntimeError("Strict TLS health check failed")
print("PASS: strict certificate-chain and hostname verification", flush=True)

def rejects(candidate: ssl.SSLContext, hostname: str) -> bool:
    try:
        with socket.create_connection(("127.0.0.1", 8443), timeout=10) as connection:
            with candidate.wrap_socket(connection, server_hostname=hostname):
                return False
    except ssl.SSLCertVerificationError:
        return True

if not rejects(context, "foreign.invalid"):
    raise RuntimeError("TLS accepted an incorrect hostname")
print("PASS: strict TLS rejects an incorrect hostname", flush=True)
untrusted = ssl.SSLContext(ssl.PROTOCOL_TLS_CLIENT)
untrusted.verify_flags |= ssl.VERIFY_X509_STRICT
if not rejects(untrusted, "localhost"):
    raise RuntimeError("TLS accepted an untrusted certificate")
print("PASS: strict TLS rejects an untrusted certificate", flush=True)
