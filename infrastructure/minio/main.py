import os
import json
import time
import pathlib
import subprocess
from typing import Optional

import boto3
import boto3.session
import requests
import typer

app = typer.Typer()

# --- Constants ---
DOCKER_COMPOSE_FILE = "./docker-compose.yml"
DEFAULT_ENV_PATH = "../../src/Olve.Trains.AssetPipeline/.env"
ENV_TEMPLATE = (
    "S3_URL={S3_URL}\n"
    "S3_BUCKET={S3_BUCKET}\n"
    "S3_KEY={S3_KEY}\n"
    "S3_SECRET={S3_SECRET}\n"
)
MINIO_HOST = "localhost"
MINIO_PORT = 9000
MINIO_ADMIN_USERNAME = "minio"
MINIO_ADMIN_PASSWORD = "minio123"
MINIO_HEALTH_ENDPOINT = f"http://{MINIO_HOST}:{MINIO_PORT}/minio/health/live"
MINIO_STARTUP_TIMEOUT = 60


# --- Logging Utilities ---
def log_info(msg: str):
    typer.secho(msg, fg=typer.colors.BLUE)


def log_warn(msg: str):
    typer.secho(f"⚠️  {msg}", fg=typer.colors.YELLOW)


def log_error(msg: str):
    typer.secho(f"❌ {msg}", fg=typer.colors.RED)


def log_success(msg: str):
    typer.secho(f"✅ {msg}", fg=typer.colors.GREEN)


def log_confirm(msg: str) -> bool:
    return typer.confirm(typer.style(msg, fg=typer.colors.GREEN))


def log_prompt_str(msg: str) -> str:
    return typer.prompt(typer.style(msg, fg=typer.colors.CYAN))


def log_prompt_bool(msg: str, default: bool = True) -> bool:
    return typer.prompt(
        typer.style(msg, fg=typer.colors.CYAN),
        default=default,
        type=bool,
        show_default=True,
        show_choices=True
    )


# --- Helpers ---
def start_minio():
    log_info("Starting MinIO via Docker Compose...")
    subprocess.run(["docker", "compose", "-f", DOCKER_COMPOSE_FILE, "up", "-d"], check=True)


def stop_minio():
    log_info("Stopping MinIO via Docker Compose...")
    subprocess.run(["docker", "compose", "-f", DOCKER_COMPOSE_FILE, "down"], check=True)


def wait_for_minio():
    log_info("Waiting for MinIO to become healthy...")
    start_time = time.time()
    while True:
        try:
            response = requests.get(MINIO_HEALTH_ENDPOINT, timeout=3)
            if response.ok:
                log_success("MinIO is up and running.")
                return
        except requests.RequestException:
            pass
        if time.time() - start_time > MINIO_STARTUP_TIMEOUT:
            raise TimeoutError("MinIO did not become healthy in time.")
        time.sleep(1)


def load_credentials_file(path: pathlib.Path) -> tuple[str, str]:
    try:
        content = path.read_text()
        credentials = json.loads(content)
        return credentials["accessKey"], credentials["secretKey"]
    except (KeyError, json.JSONDecodeError) as e:
        raise typer.BadParameter(f"Invalid credentials file: {e}")


def create_minio_client(access_key, secret_key):
    session = boto3.session.Session()
    return session.client(
        "s3",
        endpoint_url=f"http://{MINIO_HOST}:{MINIO_PORT}",
        aws_access_key_id=access_key,
        aws_secret_access_key=secret_key,
        config=boto3.session.Config(s3={"addressing_style": "path"}),
    )

def check_if_bucket_exists(client, bucket_name: str) -> bool:
    try:
        client.head_bucket(Bucket=bucket_name)
        return True
    except client.exceptions.NoSuchBucket:
        return False
    except client.exceptions.ClientError as e:
        if e.response["Error"]["Code"] == "404":
            return False
        if e.response["Error"]["Code"] == "403":
            log_warn(f"Bucket '{bucket_name}' exists but access is denied.")
            return True
        else:
            log_error(f"Error checking bucket '{bucket_name}': {e}")
            return False


def write_env_file(path, bucket_name, access_key, secret_key):
    content = ENV_TEMPLATE.format(
        S3_URL=f"http://{MINIO_HOST}:{MINIO_PORT}",
        S3_BUCKET=bucket_name,
        S3_KEY=access_key,
        S3_SECRET=secret_key,
    )
    pathlib.Path(path).write_text(content)
    log_success(f".env file written to {path}")


def read_bucket_from_env_file(env_path: pathlib.Path) -> Optional[str]:
    try:
        content = env_path.read_text()
        for line in content.splitlines():
            if line.startswith("S3_BUCKET="):
                return line.split("=")[1].strip()
        return None
    except Exception as e:
        log_error(f"Error reading .env file: {e}")
        return None


def clear_data_folder():
    data_folder = "./data"
    if pathlib.Path(data_folder).exists():
        subprocess.run(["rm", "-rf", data_folder], check=True)
        log_success(f"Cleared {data_folder} folder.")
    else:
        log_warn(f"{data_folder} folder does not exist.")


# --- CLI Commands ---
@app.command()
def up(
    credentials_path: Optional[pathlib.Path] = typer.Option(
        None, help="Path to JSON credentials file downloaded from MinIO"
    ),
    env_out: str = typer.Option(DEFAULT_ENV_PATH, help="Destination .env file path"),
    bucket: Optional[str] = typer.Option(None, help="MinIO bucket name"),
):
    """
    Start MinIO and create bucket + .env file.
    """
    os.chdir(pathlib.Path(__file__).parent.resolve())

    start_minio()
    wait_for_minio()

    log_info(f"Open http://{MINIO_HOST}:{MINIO_PORT} in your browser.")
    log_info(f"Login with username: {MINIO_ADMIN_USERNAME}, password: {MINIO_ADMIN_PASSWORD}")
    log_info("In the UI, go to Access Keys and create a new key.")
    log_info("Download the resulting JSON credentials file and save it locally.")

    if not credentials_path:
        credentials_input = log_prompt_str("Enter path to downloaded JSON credentials file")
        credentials_path = pathlib.Path(credentials_input)

    if not credentials_path.exists():
        log_error(f"File not found: {credentials_path}")
        raise typer.Exit(1)

    access_key, secret_key = load_credentials_file(credentials_path)
    client = create_minio_client(access_key, secret_key)

    try:
        client.list_buckets()
    except Exception as e:
        log_error(f"Failed to verify credentials: {e}")
        raise typer.Exit(1)

    if not bucket:
        bucket = log_prompt_str("Enter bucket name")

    if check_if_bucket_exists(client, bucket):
        log_warn(f"Bucket '{bucket}' already exists.")
        if not log_confirm("Do you want to overwrite it?"):
            raise typer.Exit(0)

    log_info(f"Creating bucket '{bucket}'...")

    try:
        client.create_bucket(Bucket=bucket)
        log_info(f"Bucket '{bucket}' created.")
    except client.exceptions.BucketAlreadyOwnedByYou:
        log_warn(f"Bucket '{bucket}' already exists.")
    except Exception as e:
        log_error(f"Failed to create bucket: {e}")
        raise typer.Exit(1)

    write_env_file(env_out, bucket, access_key, secret_key)
    log_success("MinIO setup complete.")


@app.command()
def down(env_file: str = typer.Option(DEFAULT_ENV_PATH, help="Path to .env file to remove")):
    """
    Tear down MinIO and remove .env file.
    """
    stop_minio()
    try:
        os.remove(env_file)
        log_info(f"Removed {env_file}")
    except FileNotFoundError:
        log_warn(f"{env_file} not found (already removed?)")
    log_success("Teardown complete.")


@app.command()
def clean(
    credentials_path: Optional[pathlib.Path] = typer.Option(
        None, help="Path to JSON credentials file downloaded from MinIO"
    ),
    env_path: str = typer.Option(DEFAULT_ENV_PATH, help="Path to .env file"),
):
    """
    Clean up: delete MinIO bucket, stop Docker, and clear data folder.
    """
    os.chdir(pathlib.Path(__file__).parent.resolve())

    start_minio()
    wait_for_minio()

    if not credentials_path:
        credentials_input = log_prompt_str("Enter path to downloaded JSON credentials file")
        credentials_path = pathlib.Path(credentials_input)

    if not credentials_path.exists():
        log_error(f"File not found: {credentials_path}")
        raise typer.Exit(1)

    access_key, secret_key = load_credentials_file(credentials_path)
    client = create_minio_client(access_key, secret_key)

    bucket_name = read_bucket_from_env_file(pathlib.Path(env_path))
    if not bucket_name:
        log_error("Bucket name not found in .env file.")
        raise typer.Exit(1)

    if check_if_bucket_exists(client, bucket_name):
        try:
            client.delete_bucket(Bucket=bucket_name)
            log_success(f"Bucket '{bucket_name}' deleted.")
        except Exception as e:
            log_error(f"Failed to delete bucket: {e}")
            raise typer.Exit(1)
    else:
        log_warn(f"Bucket '{bucket_name}' does not exist.")
        log_success("No action needed.")

    stop_minio()

    clear_data_folder()

    log_success("Cleanup complete.")


if __name__ == "__main__":
    app()
