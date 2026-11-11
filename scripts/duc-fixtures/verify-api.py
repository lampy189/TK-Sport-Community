"""Read-only API checks after DevLogin; does not consume manual test requests."""
import argparse
import http.cookiejar
import json
import urllib.error
import urllib.request
import time
from datetime import datetime, timezone
from pathlib import Path

parser=argparse.ArgumentParser()
parser.add_argument("--base",default="http://127.0.0.1:17207")
parser.add_argument("--manifest",default="docs/testing/duc/ductm-test-manifest.json")
parser.add_argument("--output",default="docs/testing/duc/api-verification.json")
args=parser.parse_args()
manifest=json.loads(Path(args.manifest).read_text())
checks=[]
def check(ok,label):
 if not ok: raise AssertionError(label)
 checks.append(dict(check=label,result="PASS"))
 print("PASS:",label,flush=True)
class Client:
 def __init__(self,account):
  self.http=urllib.request.build_opener(urllib.request.HTTPCookieProcessor(http.cookiejar.CookieJar()))
  self.token=self.call("/api/auth/csrf")["token"]
  self.call("/api/auth/dev-login",dict(email=account["email"],portal="player" if account["role"]=="Player" else "staff"))
  self.token=self.call("/api/auth/csrf")["token"]
 def call(self,path,body=None,expected=200):
  headers={"Content-Type":"application/json","X-TKSC-Request":"1"}
  if hasattr(self,"token"):headers["X-CSRF-TOKEN"]=self.token
  if path.startswith("/api/auth/"):time.sleep(2.1)  # Respect the existing 30/min auth limit.
  request=urllib.request.Request(args.base+path,headers=headers,data=None if body is None else json.dumps(body).encode())
  try:
   with self.http.open(request,timeout=40) as r:code,raw=r.status,r.read()
  except urllib.error.HTTPError as e:code,raw=e.code,e.read()
  if code!=expected:raise AssertionError(f"{path}: expected {expected}, got {code}; {raw[:250]!r}")
  return json.loads(raw) if raw else None
clients={}
for account in manifest["accounts"]:
 c=Client(account);clients[account["name"]]=c
 me=c.call("/api/auth/me")
 check(me["email"]==account["email"] and me["role"]==account["role"],account["name"]+" login and current role")
 if account["role"]!="Player":check(int(me["venueId"])==manifest["venueId"],account["name"]+" assigned test venue")
tickets={t["account"]:t for t in manifest["tickets"]}
for name,ticket in tickets.items():
 c=clients[name];rows=c.call("/api/v1/commerce/payment/tickets")
 check(any(row["id"]==ticket["ticketId"] for row in rows),name+" sees own confirmed test ticket")
 data=c.call(f'/api/ticket-aftercare/tickets/{ticket["ticketId"]}/messages')
 check(not data["messages"],name+" conversation remains empty for manual tests")
other=clients["ductmtest11"]
other.call(f'/api/ticket-aftercare/tickets/{tickets["ductmtest06"]["ticketId"]}/messages',expected=404)
check(True,"unrelated Player cannot read another ticket conversation")
other.call("/api/ticket-aftercare/inbox",expected=403)
check(True,"Player cannot read Staff inbox")
for name in ["ductmtest01","ductmtest02"]:
 c=clients[name]
 for endpoint in ["sessions","incidents","reviews","changes","refunds","inbox"]:
  data=c.call("/api/ticket-aftercare/"+endpoint)
  check(isinstance(data,list),name+" loads "+endpoint)
 rows=c.call("/api/ticket-aftercare/sessions")
 check(all(any(row["id"]==s["id"] for row in rows) for s in manifest["sessions"] if s["key"]!="assessment"),name+" sees test sessions")
for name in ["ductmtest01","ductmtest03","ductmtest04","ductmtest05"]:
 check(clients[name].call("/api/v1/skill-reviews/exceptions")==[],name+" exceptions untouched")
for name in ["ductmtest09","ductmtest10"]:
 c=clients[name]
 check(c.call("/api/v1/skill-reviews/appeals")==[],name+" appeals untouched")
 c.call("/api/skills/me")
 expected=next(a["assessmentId"] for a in manifest["assessments"] if a["account"]==name)
 # Public overview includes sport IDs; use published result endpoint for exact assessment.
 found=False
 for sport_id in [1,2]:
  rows=c.call(f"/api/v1/skill/my-results?sportId={sport_id}")
  if any(row["assessmentId"]==expected for row in rows):found=True
 check(found,name+" can select published assessment for appeal")
Path(args.output).write_text(json.dumps(dict(checkedAt=datetime.now(timezone.utc).isoformat(),base=args.base,mode="DevLogin + GET; no manual workflows consumed",passed=len(checks),checks=checks),ensure_ascii=False,indent=2)+"\n")
print("ALL PASS:",len(checks))
